#nullable enable

using System;
using UnityEngine;
using UniRx;

namespace Hedwig.RTSCore
{
    public enum MouseMoveEventType
    {
        Enter,
        Exit,
        Over
    }

    public struct MouseMoveEvent {
        public MouseMoveEventType type;
        public Vector3 position;
    }

    public interface IMouseOperation
    {
        IObservable<Unit> OnLeftClick { get; }
        IObservable<bool> OnLeftTrigger { get; }
        IObservable<Unit> OnRightClick { get; }
        IObservable<bool> OnRightTrigger { get; }

        IObservable<MouseMoveEvent> OnMove { get; }
    }
}
