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
    public interface ILauncherController
    {
        ITransform mazzle { get; }
        void Initialize(ILauncher launcher);
    }

    public interface ILauncherHandler : IDisposable
    {
        void Fire(ITransform start, ITransform target);
        void TriggerOn(ITransform start, ITransform target);
        void TriggerOff();
        void Error();
    }

    public interface ILauncherHandlerEvent
    {
        void OnShowTrajectory(bool v);
        void OnBeforeFire();
        void OnAfterFire();
        void OnFired(IProjectile projectile);
    }

    public interface ILauncher : IDisposable
    {
        void Initialize();

        ProjectileObject? projectileObject { get; }
        void SetProjectile(ProjectileObject? projectileObject, ProjectileOption? option = null);
        UniTask SetProjectileAsync(ProjectileObject? projectileObject, ProjectileOption? option = null, CancellationToken cancellationToken=default);

        ITransformProvider? target { get; }
        void SetTarget(ITransformProvider? target);

        IReadOnlyReactiveProperty<bool> CanFire { get; }
        void Fire();
        void TriggerOn();
        void TriggerOff();

        IObservable<ProjectileObject?> OnProjectilehanged { get; }
        IObservable<ITransformProvider?> OnTargetChanged { get; }
        IObservable<float> OnRecastTimeUpdated { get; }
        IObservable<IProjectile> OnFired { get; }
    }
}
