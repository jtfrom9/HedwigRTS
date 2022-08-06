#nullable enable

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Controller
{
    public class NavMeshAgentEnemyController : ControllerBase, IEnemyController, IVisualProperty, IHitHandler
    {
        string _name = "";
        IEnemyControllerEvent? controllerEvent;
        ITransform _transform = new CachedTransform();
        NavMeshAgent? _agent;
        Rigidbody? _rigidbody;

        Vector3 initialPosition;
        Quaternion initialRotation;
        Vector3 initialScale;
        float? _distanceToGround;

        CancellationTokenSource cts = new CancellationTokenSource();

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
                transform.position = position.Value;
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

        #region IMobileObject
        ITransform ITransformProvider.transform { get => _transform; }
        #endregion

        #region ICharactor
        float IVisualProperty.distanceToGround
        {
            get {
                if(_distanceToGround==null) {
                    var mr = GetComponent<MeshRenderer>();
                    _distanceToGround = mr.bounds.extents.y;
                }
                return _distanceToGround.Value;
            }
        }
        #endregion

        #region IEnemyController
        string IEnemyController.name { get => _name; }
        void IEnemyController.SetDestination(Vector3 pos)
        {
            _agent!.isStopped = false;
            _agent?.SetDestination(pos);
        }
        void IEnemyController.Stop()
        {
            _agent!.isStopped = true;
            _agent?.SetDestination(_transform.Position);
        }
        void IEnemyController.ResetPos()
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            transform.localScale = initialScale;
        }
        void IEnemyController.Knockback(Vector3 direction, float power)
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

        IVisualProperty IEnemyController.GetProperty()
        {
            return this;
        }

        static int count = 0;
        [RuntimeInitializeOnLoadMethod]
        void _InitializeOnEnterPlayMode()
        {
            count = 0;
        }

        void IEnemyController.Initialize(string name, IEnemyControllerEvent controllerEvent, Vector3? position)
        {
            if (gameObject.name == "")
            {
                gameObject.name = $"{name}_{count}";
                count++;
            }
            _name = gameObject.name;
            this.controllerEvent = controllerEvent;
            this.initialize(position);
        }
        #endregion
    }
}