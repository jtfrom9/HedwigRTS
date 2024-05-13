#nullable enable

using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;

namespace Hedwig.RTSCore
{
    public interface ILauncherHandlerEvent
    {
        void OnShowTrajectory(bool v);
        void OnBeforeFire();
        void OnAfterFire();
        void OnFired(IProjectile projectile);
    }

    public interface ILauncherHandler : IDisposable
    {
        void Fire(ITransform start, ITransform target);
        void TriggerOn(ITransform start, ITransform target);
        void TriggerOff();
        void Error();
    }

    public interface ILauncherController
    {
        ITransform Mazzle { get; }
        void Initialize(ILauncher launcher);
    }

    public interface ILauncher : IDisposable
    {
        void Initialize();

        IProjectileData? ProjectileData { get; }
        void SetProjectile(IProjectileData? projectileObject, ProjectileOption? option = null);
        UniTask SetProjectileAsync(IProjectileData? projectileObject, ProjectileOption? option = null, CancellationToken cancellationToken=default);

        ITransformProvider? Target { get; }
        void SetTarget(ITransformProvider? target);

        IReadOnlyReactiveProperty<bool> CanFire { get; }
        void Fire();
        void TriggerOn();
        void TriggerOff();

        IObservable<IProjectileData?> OnProjectileChanged { get; }
        IObservable<ITransformProvider?> OnTargetChanged { get; }
        IObservable<float> OnRecastTimeUpdated { get; }
        IObservable<IProjectile> OnFired { get; }
    }
}
