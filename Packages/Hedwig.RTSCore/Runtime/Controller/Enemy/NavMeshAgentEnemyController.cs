#nullable enable

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using UnityEditorInternal;
using UniRx;

namespace Hedwig.RTSCore.Controller
{
    public class NavMeshAgentEnemyController : ControllerBase, IUnitController, IVisualProperty, IHitHandler
    {
        string _name;
        IUnitControllerCallback? controllerEvent;
        ITransform _transform = new CachedTransform();
        NavMeshAgent? _agent;
        Rigidbody? _rigidbody;

        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3 initialScale;
        float? _distanceToGround;
        float? _distanceToHead;

        CancellationTokenSource cts = new CancellationTokenSource();

        [SerializeField]
        string CurrentState;
        [SerializeField]
        string Target;

        void Awake()
        {
            _transform.Initialize(transform);
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = 3;

            _rigidbody = GetComponent<Rigidbody>();

            var mr = GetComponent<MeshRenderer>();
            mr.material.color = UnityEngine.Random.ColorHSV();
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

        void initialize(Vector3? position)
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
            controllerEvent?.OnHit(hitObject);
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
            _agent!.isStopped = false;
            _agent?.SetDestination(pos);
        }
        void IUnitController.Stop()
        {
            _agent!.isStopped = true;
            _agent?.SetDestination(_transform.Position);
        }
        void IUnitController.ResetPos()
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            transform.localScale = initialScale;
        }
        void IUnitController.Knockback(Vector3 direction, float power)
        {
            if (cts.IsCancellationRequested)
                return;
            Debug.Log($"{_name}: AddShock: ${direction}, ${power}");
            if (_rigidbody != null && _agent!=null)
            {
                UniTask.Create(async () => {
                    _agent.isStopped = true;
                    _rigidbody.isKinematic = false;
                    _rigidbody.AddForce(direction * power, ForceMode.Impulse);

                    await UniTask.Delay(1000, cancellationToken: cts.Token);
                    _rigidbody.isKinematic = true;
                    _agent.isStopped = false;
                }).Forget();
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
        }

        void IUnitController.Initialize(IUnitControllerCallback controllerEvent, Vector3? position, string? name)
        {
            if (name != null)
            {
                gameObject.name = name;
            }
            else
            {
                _name = gameObject.name;
            }
            this.controllerEvent = controllerEvent;
            this.initialize(position);
        }
        #endregion
    }
}