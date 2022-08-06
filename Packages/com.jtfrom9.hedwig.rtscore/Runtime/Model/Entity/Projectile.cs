#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    namespace Projectile
    {
        public enum Status
        {
            Init, Active, End
        }

        public enum EndReason
        {
            CharactorHit,
            OtherHit,
            Expired,
            Disposed
        }

        public enum EventType
        {
            BeforeMove,
            AfterMove,
            Trigger,
            WillHit,
            BeforeLastMove,
            AfterLastMove,
            OnKill,
            OnComplete,
            OnPause,
            Destroy
        }

        public struct EventArg {
            public EventType type;
            public Collider? collider;
            public RaycastHit? willHit;
            public EndReason? endReason;
            public Ray? ray;
            public float? maxDistance;
            public Vector3? to;
            public float? speed;

            public EventArg(EventType type)
            {
                this.type = type;
                this.collider = null;
                this.willHit = null;
                this.endReason = null;
                this.ray = null;
                this.maxDistance = null;
                this.to = null;
                this.speed = null;
            }
        }
    }

    public interface IProjectileController : ITransformProvider
    {
        void Initialize(string name, Vector3 initial);

        string name { get; }
        UniTask<bool> Move(Vector3 to, float speed);
        UniTask LastMove(float speed);
        IObservable<Projectile.EventArg> OnEvent { get; }
    }

    public struct ProjectileOption
    {
        public bool? destroyAtEnd;

        public bool DestroyAtEnd { get => destroyAtEnd ?? true; }
    }

    public interface IProjectile: IDisposable
    {
        IProjectileController controller { get; }
        Projectile.EndReason EndReason { get; }

        IObservable<Unit> OnStarted { get; }
        IObservable<Unit> OnEnded { get; }
        IObservable<Unit> OnDestroy { get; }
        void Start(ITransform target, in ProjectileOption? option = null);

        TrajectoryMap? trajectoryMap { get; }
    }

    public interface IProjectileFactory
    {
        IProjectile? Create(Vector3 start);
        IObservable<IProjectile> OnCreated { get; }
    }
}
