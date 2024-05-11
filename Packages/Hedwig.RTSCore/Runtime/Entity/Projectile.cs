#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public enum ProjectileType
    {
        Fire,
        Burst,
        Grenade
    };

    public interface IProjectileData
    {
        string Name { get; }
        ProjectileType Type { get; }
        bool Chargable { get; }
        int SuccessionCount { get; }
        int SuccessionInterval { get; }
        int RecastTime { get; }
        float Shake { get; }
        float BaseSpeed { get; }
        float Range { get; }
        IWeaponData? WeaponData { get; }

        Vector3[] MakePoints(Vector3 from, Vector3 to);
    }

    public enum ProjectileStatus
    {
        Init, Active, End
    }

    public enum ProjectileEndReason
    {
        CharactorHit,
        OtherHit,
        Expired,
        Disposed
    }

    public enum ProjectileEventType
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

    public struct ProjectileEventArg
    {
        public ProjectileEventType type;
        public Collider? collider;
        public RaycastHit? willHit;
        public ProjectileEndReason? endReason;
        public Ray? ray;
        public float? maxDistance;
        public Vector3? to;
        public float? speed;

        public ProjectileEventArg(ProjectileEventType type)
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

    public interface IProjectileController : ITransformProvider
    {
        void Initialize(string name, Vector3 initial);

        string name { get; }
        UniTask<bool> Move(Vector3 to, float speed);
        UniTask LastMove(float speed);
        IObservable<ProjectileEventArg> OnEvent { get; }
    }

    public struct ProjectileOption
    {
        public bool? destroyAtEnd;

        public bool DestroyAtEnd { get => destroyAtEnd ?? true; }
    }

    public interface IProjectileFactory
    {
        IProjectile? Create(Vector3 start);
        IObservable<IProjectile> OnCreated { get; }
    }

    public interface IProjectile: IDisposable
    {
        IProjectileController controller { get; }
        ProjectileEndReason EndReason { get; }

        IObservable<Unit> OnStarted { get; }
        IObservable<Unit> OnEnded { get; }
        IObservable<Unit> OnDestroy { get; }

        void Start(ITransform target, in ProjectileOption? option = null);
    }

    public interface ITrajectoryVisualizer
    {
        bool visible { get; }
        void SetStartTarget(ITransform? target);
        void SetEndTarget(ITransform? target);
        void SetProjectile(IProjectileData? projectileObject);
        void Show(bool v);
    }
}
