#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using DG.Tweening;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore.Controller
{
    using Projectile;

    public class TweenProjectileController : MonoBehaviour, IProjectileController
    {
        [SerializeField]
        [Min(1)]
        float castingEveryFrameSpeed = 50;

        ITransform _transform = new CachedTransform();
        string _name = "";
        bool _disposed = false;

        bool _willHit = false;
        bool _hit = false;
        RaycastHit? willCastHit = null;
        float _lastSpeed = 0f;

        Subject<Projectile.EventArg> onEvent = new Subject<EventArg>();

        void Awake() {
            _transform.Initialize(transform);
        }

        void OnDestroy()
        {
            _disposed = true;
            onEvent.OnNext(new EventArg(Projectile.EventType.Destroy));
            onEvent.OnCompleted();
        }

        void OnTriggerEnter(Collider collider)
        {
            if (_hit) return;

            EndReason? endReason = null;
            if (collider.gameObject.CompareTag(HitTag.Character))
            {
                endReason = EndReason.CharactorHit;
                _hit = true;
            }
            if (collider.gameObject.CompareTag(HitTag.Environment))
            {
                endReason = EndReason.OtherHit;
                _hit = true;
            }
            if (_hit)
            {
                onEvent.OnNext(new EventArg(Projectile.EventType.Trigger)
                {
                    collider = collider,
                    willHit = willCastHit,
                    endReason = endReason,
                    speed = _lastSpeed
                });
                // request cancel
                _transform.Raw.DOKill();
            }
        }

        void willHit(GameObject gameObject, Ray ray, float distance, RaycastHit hit)
        {
            var mobileObject = gameObject.GetComponent<ITransformProvider>();
            if (mobileObject != null)
            {
                // immediately stop tweening without cancel
                _willHit = true;
                _transform.Raw.DOKill();
                onEvent.OnNext(new EventArg(Projectile.EventType.WillHit)
                {
                    willHit = hit,
                    ray = ray,
                    maxDistance = distance
                });
            }
        }

        void hitHandler(Ray ray, float distance, RaycastHit hit)
        {
            var gameObject = hit.collider.gameObject;
            if (gameObject.CompareTag(HitTag.Character) ||
                gameObject.CompareTag(HitTag.Environment))
            {
                willHit(gameObject, ray, distance, hit);
            }
        }

        void hitTest(Vector3 pos, Vector3 dir, float speed) {
            // var hits = Physics.RaycastAll(pos, dir, speed * Time.deltaTime );
            // if(hits.Length > 0) {
            //     Debug.Log($"hits: {hits.Length}");
            //     RaycastHit? nearest = null;
            //     foreach(var hit in hits) {
            //         if (!nearest.HasValue) { nearest = hit; }
            //         else {
            //             if(nearest.Value.distance > hit.distance)
            //                 nearest = hit;
            //         }
            //     }
            //     hitHandler(nearest!.Value);
            // }

            var hit = new RaycastHit();
            var ray = new Ray(pos, dir);
            var distance = speed * Time.deltaTime;
            if (Physics.Raycast(ray, out hit, distance))
            {
                this.willCastHit = hit;
                hitHandler(ray, distance, hit);
            }
        }


        async UniTask<bool> move(Vector3 to, float speed)
        {
            _transform.Raw.rotation = Quaternion.LookRotation(to - _transform.Position);
            _lastSpeed = speed;

            var castEveryFrame = speed > castingEveryFrameSpeed;
            onEvent.OnNext(new EventArg(Projectile.EventType.BeforeMove)
            {
                to = to
            });

            var dir = (to - _transform.Position).normalized;
            var tween = _transform.Raw.DOMove(to, speed)
                .SetUpdate(UpdateType.Fixed)
                .SetEase(Ease.Linear)
                .SetSpeedBased(true)
                .OnKill(() =>
                {
                    onEvent.OnNext(new EventArg(Projectile.EventType.OnKill));
                })
                .OnComplete(() =>
                {
                    onEvent.OnNext(new EventArg(Projectile.EventType.OnComplete));
                })
                .OnPause(() =>
                {
                    onEvent.OnNext(new EventArg(Projectile.EventType.OnPause));
                });

            if (castEveryFrame)
            {
                tween = tween.OnUpdate(() => hitTest(_transform.Position, dir, speed));
            }
            await tween;

            onEvent.OnNext(new EventArg(Projectile.EventType.AfterMove));
            return _willHit || _hit;
        }

        async UniTask lastMove(float speed)
        {
            //
            // last one step move to the object will hit
            //
            if (willCastHit.HasValue && !_hit)
            {
                onEvent.OnNext(new EventArg(Projectile.EventType.BeforeLastMove)
                {
                    willHit = willCastHit
                });

                // move to will hit point
                await _transform.Raw.DOMove(willCastHit.Value.point, speed)
                    .SetSpeedBased(true)
                    .SetEase(Ease.Linear)
                    .SetUpdate(UpdateType.Fixed);
                // at this timing, onTrigger caused because hit will be supporsed,
                // one more frame wait is needed to update last move
                await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);

                onEvent.OnNext(new EventArg(Projectile.EventType.AfterLastMove));
            }
        }

        void dispose()
        {
            if (_disposed) return;
            // _disposed = true;

            if (DOTween.IsTweening(transform))
            {
                _transform.Raw.DOKill();
            }
            Destroy(gameObject);
        }

        #region IDisposable
        void IDisposable.Dispose()
        {
            dispose();
        }
        #endregion

        #region IMobileObject
        ITransform ITransformProvider.transform { get => _transform; }
        #endregion

        #region IProjectileController
        string IProjectileController.name { get => _name; }
        UniTask<bool> IProjectileController.Move(Vector3 to, float speed) => move(to, speed);
        UniTask IProjectileController.LastMove(float speed) => lastMove(speed);
        IObservable<Projectile.EventArg> IProjectileController.OnEvent { get => onEvent; }

        static int count = 0;

        [RuntimeInitializeOnLoadMethod]
        void _InitializeOnEnterPlayMode()
        {
            count = 0;
        }

        void IProjectileController.Initialize(string name, Vector3 initial)
        {
            if (gameObject.name == "")
            {
                gameObject.name = $"{name}_{count}";
                count++;
            }
            this._name = gameObject.name;
            transform.position = initial;
        }
        #endregion

    }
}