#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public interface ITrajectoryLineMap
    {
        float FromFactor { get; }
        float ToFactor { get; }
        bool IsFirst { get; }

        Vector3 GetFromPoint();
        Vector3 GetToPoint();
        float GetAccelatedSpeed();
        (Vector3, Vector3) GetPoints() => (GetFromPoint(), GetToPoint());
    }

    public interface ITrajectorySectionMap
    {
        float AdjustMaxAngle { get; }
        int NumLines { get; }
        float Speed { get; }
        float AdditionalSpeed { get; }

        bool IsCurve { get; }
        bool IsHoming { get; }

        IEnumerable<ITrajectoryLineMap> Lines { get; }

        void AddDynamicLine(int index,
            float fromFactor,
            float totFactor);
    }

    public interface ITrajectoryMap
    {
        IEnumerable<ITrajectorySectionMap> Sections { get; }
        IEnumerable<ITrajectoryLineMap> Lines { get; }
    }

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

        bool HasMap { get; }
        ITrajectoryMap ToMap(Vector3 from, Vector3 to);
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
        public ProjectileEventType Type;
        public Collider? Collider;
        public RaycastHit? WillHit;
        public ProjectileEndReason? EndReason;
        public Ray? Ray;
        public float? MaxDistance;
        public Vector3? To;
        public float? Speed;

        public ProjectileEventArg(ProjectileEventType type)
        {
            this.Type = type;
            this.Collider = null;
            this.WillHit = null;
            this.EndReason = null;
            this.Ray = null;
            this.MaxDistance = null;
            this.To = null;
            this.Speed = null;
        }
    }

    public interface IProjectileController : ITransformProvider
    {
        void Initialize(IProjectile projectile, Vector3 initial, string? name);

        string Name { get; }
        UniTask<bool> Move(Vector3 to, float speed);
        UniTask LastMove(float speed);
        IObservable<ProjectileEventArg> OnEvent { get; }
    }

    public struct ProjectileOption
    {
        public bool? destroyAtEnd;

        public bool DestroyAtEnd { get => destroyAtEnd ?? true; }
    }

    public interface IProjectile: IDisposable
    {
        IProjectileController Controller { get; }
        ProjectileEndReason EndReason { get; }

        IObservable<Unit> OnStarted { get; }
        IObservable<Unit> OnEnded { get; }
        IObservable<Unit> OnDestroy { get; }

        void Start(ITransform target, in ProjectileOption? option = null);
    }

    public delegate IProjectile? IProjectileFactory(IProjectileData projectileData, Vector3 start, string? name);

    public interface ITrajectoryVisualizer
    {
        bool Visible { get; }
        void SetStartTarget(ITransform? target);
        void SetEndTarget(ITransform? target);
        void SetProjectile(IProjectileData? projectileObject);
        void Show(bool v);
    }
}
