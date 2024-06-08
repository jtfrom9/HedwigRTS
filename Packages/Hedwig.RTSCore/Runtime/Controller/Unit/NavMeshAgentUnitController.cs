#nullable enable

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using Cysharp.Threading.Tasks;
using UniRx;
using VContainer;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Controller
{
    public class NavMeshAgentUnitController : ControllerBase, IUnitController, IVisualProperty, IHitHandler
    {
        readonly ITransform _transform = new CachedTransform();
        readonly CancellationTokenSource cts = new CancellationTokenSource();

        string _name = "";
        IUnitControllerCallback? _callback;
        NavMeshAgent? _agent;
        Rigidbody? _rigidbody;
        Collider? _collider;

        bool _timePaused = false;
        Vector3 _velocityBackup = default;

        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3 initialScale;
        float? _distanceToGround;
        float? _distanceToHead;

        [SerializeField, ReadOnly]
        string? CurrentState;
        [SerializeField, ReadOnly]
        string? Target;

        [Inject]
        readonly ITimeManager? _timeManager;

        ILauncherController? _launcherContorller = null;

        void Awake()
        {
            _transform.Initialize(transform);
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 0;

            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            var mr = GetComponent<MeshRenderer>();
            mr.material.color = UnityEngine.Random.ColorHSV();

            _launcherContorller = GetComponentInChildren<ILauncherController>();
        }

        void OnDestroy()
        {
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag(HitTag.Projectile))
            {
                var projectile = other.gameObject.GetComponent<IProjectileController>();
                var position = other.ClosestPointOnBounds(_transform.Position);
                Debug.Log($"[{projectile.GetHashCode():x}] frame:{Time.frameCount} Hit({gameObject.name}) @{position}");
            }
        }

        void initialize(IUnitData unitData, Vector3? position)
        {
            if (position.HasValue)
            {
                _agent!.enabled = false;
                transform.position = position.Value;
                _agent!.enabled = true;
            }
            this.initialPosition = transform.position;
            this.initialRotation = transform.rotation;
            this.initialScale = transform.localScale;

            _agent!.speed = unitData.Speed;
        }

        void pause(bool pause)
        {
            if (_agent == null) return;
            if (pause)
            {
                _timePaused = true;
                _agent.isStopped = true;
                _velocityBackup = _agent.velocity;
                _agent.velocity = Vector3.zero;
            }
            else
            {
                _timePaused = false;
                _agent.isStopped = false;
                _agent.velocity = _velocityBackup;
            }
        }

        // void OnTriggerStay(Collider other)
        // {
        //     if (other.gameObject.CompareTag(Collision.ProjectileTag))
        //     {
        //         var projectile = other.gameObject.GetComponent<IProjectile>();
        //         var posision = other.ClosestPointOnBounds(this.transform.position);
        //         onTrigger(projectile, posision);
        //     }
        // }

        #region IDisposable
        void IDisposable.Dispose()
        {
            cts.Cancel();
            Destroy(gameObject);
        }
        #endregion

        #region
        void IHitHandler.OnHit(IHitObject hitObject)
        {
            _callback?.OnHit(hitObject);
        }
        #endregion

        #region ITransformProvider
        ITransform ITransformProvider.Transform { get => _transform; }
        #endregion

        #region IVisualProperty
        float IVisualProperty.DistanceToGround
        {
            get {
                if(_distanceToGround==null) {
                    var mr = GetComponent<MeshRenderer>();
                    _distanceToGround = mr.bounds.extents.y;
                }
                return _distanceToGround.Value;
            }
        }

        float IVisualProperty.DistanceToHead
        {
            get
            {
                if (_distanceToHead==null)
                {
                    var mr = GetComponent<MeshRenderer>();
                    _distanceToHead = mr.bounds.extents.y;
                }
                return _distanceToHead.Value;
            }
        }
        #endregion

        #region IEnemyController
        string IUnitController.Name { get => _name; }
        void IUnitController.SetDestination(Vector3 pos)
        {
            if (_timePaused) return;
            _agent!.isStopped = false;
            _agent?.SetDestination(pos);
        }
        void IUnitController.Stop()
        {
            if (_timePaused) return;
            _agent!.isStopped = true;
            _agent?.SetDestination(_transform.Position);
        }
        void IUnitController.SetVisibility(bool v)
        {
            // gameObject.SetActive(v);
            var renderers = GetComponents<Renderer>();
            foreach (var r in renderers) r.enabled = v;
        }

        void IUnitController.ResetPos()
        {
            if (_timePaused) return;
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            transform.localScale = initialScale;
        }

        async UniTask IUnitController.Knockback(Vector3 direction, float power)
        {
            if (_timePaused) return;
            if (cts.IsCancellationRequested)
                return;
            Debug.Log($"{_name}: AddShock: {direction}, {power}");
            if (_rigidbody != null && _agent!=null && _collider != null)
            {
                await UniTask.Create(async () =>
                {
                    // _agent.isStopped = true;
                    _agent.enabled = false;
                    _rigidbody.isKinematic = false;
                    _rigidbody.useGravity = true;
                    _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                    _collider.isTrigger = false;
                    var vec = direction.normalized * power;
                    vec.y = 0;
                    Debug.Log($"AddForce: {vec}");
                    _rigidbody.AddForce(vec, ForceMode.Impulse);

                    Debug.Log($"waiting move start..");
                    while (_rigidbody.velocity.magnitude < 0.1f)
                    {
                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
                    }
                    Debug.Log($"started move: {_rigidbody.velocity.magnitude}");

                    // await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate);
                    // await UniTask.Yield(PlayerLoopTiming.FixedUpdate);

                    while (_rigidbody.velocity.magnitude > 0.1f)
                    {
                        Debug.Log($"waiting to stop: {_rigidbody.velocity.magnitude}");
                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
                        // await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate);
                        // await UniTask.NextFrame(cancellationToken: cts.Token);
                    }
                    Debug.Log("done");

                    // await UniTask.Delay(1000);

                    // await UniTask.Delay(1000, cancellationToken: cts.Token);
                    _collider.isTrigger = true;
                    _rigidbody.isKinematic = true;
                    _rigidbody.useGravity = false;
                    // _agent.isStopped = false;
                    _agent.enabled = true;
                });
            }
        }

        IVisualProperty IUnitController.GetProperty()
        {
            return this;
        }

        GameObject IUnitController.Context { get => gameObject; }

        void IUnitController.SeDebugUnit(IUnitActionRunner unit) {
            unit.OnStateChanged.Subscribe(state => {
                CurrentState = state.Name;
            }).AddTo(this);
            unit.OnTargetChanged.Subscribe(unit => {
                Target = unit?.Name ?? "";
            }).AddTo(this);
        }

        ILauncherController? IUnitController.LauncherController { get => _launcherContorller; }

        void IUnitController.Initialize(IUnitData unitData, IUnitControllerCallback callback, Vector3? position, string? name)
        {
             if (name != null)
            {
                gameObject.name = name;
            }
            this._name = gameObject.name;
            this._callback = callback;
            this.initialize(unitData, position);
            _timeManager?.Paused.SkipLatestValueOnSubscribe().Subscribe(v => pause(v)).AddTo(this);
        }
        #endregion
    }
}