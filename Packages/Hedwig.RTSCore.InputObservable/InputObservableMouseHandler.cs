#nullable enable

using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using InputObservable;
using VContainer;

namespace Hedwig.RTSCore.InputObservable
{
    public class InputObservableMouseHandler : MonoBehaviour, IMouseOperation
    {
        [SerializeField]
        Camera? _camera = null;

        [SerializeField]
        int raycastDistance = 100;

        Subject<Unit> onLeftClick = new Subject<Unit>();
        Subject<bool> onLeftTrigger = new Subject<bool>();
        Subject<Unit> onRightClick = new Subject<Unit>();
        Subject<bool> onRightTrigger = new Subject<bool>();
        Subject<MouseMoveEvent> onMove = new Subject<MouseMoveEvent>();
        Subject<Vector2> onMoveVec2 = new Subject<Vector2>();

        [Inject]
        readonly ITimeManager? timeManager;

        bool TimePaused { get => timeManager?.Paused.Value ?? false; }

        void Start()
        {
            if (_camera == null)
            {
                Debug.LogError("no camera");
                return;
            }
            var context = this.DefaultInputContext();
            setupButton(context.GetObservable(0), context.GetObservable(1));
            setupMove(_camera);
        }

        void setupButton(IInputObservable lmb, IInputObservable rmb)
        {
            // lmb: left mouse button
            lmb.OnBegin
                .Where(_ => !TimePaused)
                .Subscribe(_ =>
                {
                    onLeftClick.OnNext(Unit.Default);
                }).AddTo(this);

            lmb.OnBegin.First().TakeUntil(lmb.OnEnd).Repeat()
                .Where(_ => !TimePaused)
                .Subscribe(_ =>
                {
                    onLeftTrigger.OnNext(true);
                }).AddTo(this);
            lmb.OnEnd
                .Where(_ => !TimePaused)
                .Subscribe(_ =>
                {
                    onLeftTrigger.OnNext(false);
                }).AddTo(this);
            lmb.Difference()
                .Where(_ => !TimePaused)
                .Where(_ => lmb.IsBegin)
                .Subscribe(v =>
                {
                    onMoveVec2.OnNext(v);
                }).AddTo(this);

            // rmb: right mouse button
            rmb.OnBegin
                .Where(_ => !TimePaused)
                .Subscribe(_ =>
                {
                    onRightClick.OnNext(Unit.Default);
                }).AddTo(this);
            rmb.OnBegin.First().TakeUntil(lmb.OnEnd).Repeat()
                .Where(_ => !TimePaused)
                .Subscribe(_ =>
                {
                    onRightTrigger.OnNext(true);
                }).AddTo(this);
            rmb.OnEnd
                .Where(_ => !TimePaused)
                .Subscribe(_ => {
                    onRightTrigger.OnNext(false);
                }).AddTo(this);
        }

        void setupMove(Camera camera)
        {
            var enter = false;
            var lastPos = Vector3.zero;
            this.UpdateAsObservable().Where(_ => !TimePaused).Subscribe(_ =>
            {
                var pos = Input.mousePosition;
                var ray = camera.ScreenPointToRay(pos);
                var hits = Physics.RaycastAll(ray, raycastDistance);
                var highestY = 0f;
                Vector3? result = null;
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.CompareTag(HitTag.Environment))
                    {
                        var y = hit.point.y;
                        if (result == null || y > highestY)
                        {
                            result = hit.point;
                            highestY = y;
                        }
                    }
                }
                if (result.HasValue)
                {
                    lastPos = result.Value;
                    if (!enter)
                    {
                        onMove.OnNext(new MouseMoveEvent()
                        {
                            type = MouseMoveEventType.Enter,
                            position = result.Value
                        });
                    } else {
                        onMove.OnNext(new MouseMoveEvent()
                        {
                            type = MouseMoveEventType.Over,
                            position = result.Value
                        });
                    }
                    enter = true;
                } else {
                    if(enter) {
                        onMove.OnNext(new MouseMoveEvent()
                        {
                            type = MouseMoveEventType.Exit,
                            position = lastPos
                        });
                    }
                    enter = false;
                }
            }).AddTo(this);
        }

        #region IMouseOperation
        IObservable<Unit> IMouseOperation.OnLeftClick { get => onLeftClick; }
        IObservable<bool> IMouseOperation.OnLeftTrigger { get => onLeftTrigger; }
        IObservable<Unit> IMouseOperation.OnRightClick { get => onRightClick; }
        IObservable<bool> IMouseOperation.OnRightTrigger { get => onRightTrigger; }
        IObservable<MouseMoveEvent> IMouseOperation.OnMove { get => onMove; }
        IObservable<Vector2> IMouseOperation.OnMoveVec2 { get => onMoveVec2; }
        #endregion
    }
}