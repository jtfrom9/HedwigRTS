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
    public class TweenProjectileController : MonoBehaviour, IProjectileController
    {
        [SerializeField]
        [Min(1)]
        float castingEveryFrameSpeed = 50;

        readonly ITransform _transform = new CachedTransform();
        string _name;

        bool _disposed = false;
        bool _willHit = false;
        bool _hit = false;
        RaycastHit? _raycastHit = null;
        float _lastSpeed = 0f;
        bool _timePaused = false;

        Subject<ProjectileEventArg> onEvent = new Subject<ProjectileEventArg>();

        void Awake() {
            _transform.Initialize(transform);
        }

        void OnDestroy()
        {
            _disposed = true;
            onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.Destroy));
            onEvent.OnCompleted();
        }

        void OnTriggerEnter(Collider collider)
        {
            if (_hit) return;

            ProjectileEndReason? endReason = null;
            if (collider.gameObject.CompareTag(HitTag.Character))
            {
                endReason = ProjectileEndReason.CharactorHit;
                _hit = true;
            }
            if (collider.gameObject.CompareTag(HitTag.Environment))
            {
                endReason = ProjectileEndReason.OtherHit;
                _hit = true;
            }
            if (_hit)
            {
                onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.Trigger)
                {
                    Collider = collider,
                    WillHit = _raycastHit,
                    EndReason = endReason,
                    Speed = _lastSpeed
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
                onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.WillHit)
                {
                    WillHit = hit,
                    Ray = ray,
                    MaxDistance = distance
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
                this._raycastHit = hit;
                hitHandler(ray, distance, hit);
            }
        }


        async UniTask<bool> move(Vector3 to, float speed)
        {
            _transform.Raw.rotation = Quaternion.LookRotation(to - _transform.Position);
            _lastSpeed = speed;

            var castEveryFrame = speed > castingEveryFrameSpeed;
            onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.BeforeMove)
            {
                To = to
            });

            var dir = (to - _transform.Position).normalized;
            var tween = _transform.Raw.DOMove(to, speed)
                .SetUpdate(UpdateType.Fixed)
                .SetEase(Ease.Linear)
                .SetSpeedBased(true)
                .OnKill(() =>
                {
                    onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.OnKill));
                })
                .OnComplete(() =>
                {
                    onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.OnComplete));
                })
                .OnPause(() =>
                {
                    onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.OnPause));
                });

            if (castEveryFrame)
            {
                tween = tween.OnUpdate(() => hitTest(_transform.Position, dir, speed));
            }
            await tween;

            onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.AfterMove));
            return _willHit || _hit;
        }

        async UniTask lastMove(float speed)
        {
            //
            // last one step move to the object will hit
            //
            if (_raycastHit.HasValue && !_hit)
            {
                onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.BeforeLastMove)
                {
                    WillHit = _raycastHit
                });

                // move to will hit point
                await _transform.Raw.DOMove(_raycastHit.Value.point, speed)
                    .SetSpeedBased(true)
                    .SetEase(Ease.Linear)
                    .SetUpdate(UpdateType.Fixed);
                // at this timing, onTrigger caused because hit will be supporsed,
                // one more frame wait is needed to update last move
                await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);

                onEvent.OnNext(new ProjectileEventArg(ProjectileEventType.AfterLastMove));
            }
        }

        void pause(bool paused)
        {
            void setTweenTimescale(float v)
            {
                var tweens = DOTween.TweensByTarget(_transform.Raw);
                if (tweens != null)
                {
                    foreach (var tween in tweens)
                    {
                        tween.timeScale = v;
                    }
                }
            }
            if (paused)
            {
                setTweenTimescale(0);
                _timePaused = true;
            }
            else
            {
                setTweenTimescale(1f);
                _timePaused = false;
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
        ITransform ITransformProvider.Transform { get => _transform; }
        #endregion

        #region IProjectileController
        string IProjectileController.Name { get => _name; }
        UniTask<bool> IProjectileController.Move(Vector3 to, float speed) => move(to, speed);
        UniTask IProjectileController.LastMove(float speed) => lastMove(speed);
        IObservable<ProjectileEventArg> IProjectileController.OnEvent { get => onEvent; }

        void IProjectileController.Initialize(Vector3 initial, string? name, ITimeManager? timeManager)
        {
            if (name != null)
            {
                gameObject.name = name;
            }
            _name = gameObject.name;
            transform.position = initial;
            timeManager?.Paused.SkipLatestValueOnSubscribe().Subscribe(v => pause(v)).AddTo(this);
        }
        #endregion

    }
}