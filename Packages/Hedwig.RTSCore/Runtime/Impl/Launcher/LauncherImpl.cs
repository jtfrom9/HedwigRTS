#nullable enable

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore.Impl
{
    public class LauncherImpl : ILauncher, ILauncherHandlerCallback
    {
        readonly ILauncherController _launcherController;
        readonly ITimeManager? _timeManager;
        readonly ITrajectoryVisualizer? _trajectoryVisualizer;

        readonly CompositeDisposable _disposable = new();
        readonly ReactiveProperty<bool> canFire = new();
        readonly Subject<IProjectileData?> onProjectileChanged = new();
        readonly Subject<ITransformProvider?> onTargetChanged = new();
        readonly Subject<float> onRecastTimeUpdated = new();
        readonly Subject<IProjectile> onFired = new();

        bool recasting = false;
        bool triggerReq = false;
        bool triggered = false;

        ILauncherHandler? _launcherHandler;
        IProjectileData? _projectileData;
        ITransformProvider? _target;

        void initialize()
        {
            if (_trajectoryVisualizer != null)
            {
                _trajectoryVisualizer.SetStartTarget(_launcherController.Mazzle);
            }
            _launcherController.Initialize(this);
        }

        void setCanFire()
        {
            var v = _projectileData != null &&
                _target != null &&
                _launcherHandler !=null &&
                !recasting;
            canFire.Value = v;
        }

        void changeRecastState(bool v)
        {
            recasting = v;
            setCanFire();
        }

        async UniTask stepRecast(int recast, int step, int index)
        {
            await UniTask.Delay(step);
            var elapsed = index * step;
            onRecastTimeUpdated.OnNext((float)elapsed / (float)recast);
        }

        IProjectile createProjectile(Vector3 position, string? name)
        {
            if (_projectileData is IProjectileFactory factory)
            {
                return factory.Create(position, name, _timeManager);
            }
            else
            {
                throw new InvalidCastException("Failed to createProjectile");
            }
        }

        void setProjectileCore(IProjectileData? projectileData, ProjectileOption? option)
        {
            this._projectileData = projectileData;
            this._trajectoryVisualizer?.SetProjectile(projectileData);

            // reset laouncher handler
            this._launcherHandler?.Dispose();
            this._launcherHandler = null;

            if (projectileData != null)
            {
                switch (projectileData.Type)
                {
                    case ProjectileType.Fire:
                        this._launcherHandler = new ShotLauncherHandler(this, projectileData, option);
                        break;
                    case ProjectileType.Burst:
                        this._launcherHandler = new BurstLauncherHandler(this, projectileData);
                        break;
                    case ProjectileType.Grenade:
                        this._launcherHandler = new GrenadeLauncherHandler(this, projectileData);
                        break;
                }
            }
            setCanFire();
            handleError();
            onProjectileChanged.OnNext(projectileData);
        }

        void setProjectile(IProjectileData? projectileData, ProjectileOption? option)
        {
            if (recasting) {
                throw new InvalidConditionException("LauncherManager is Recasting");
            }
            setProjectileCore(projectileData, option);
        }

        async UniTask setProjectileAsync(IProjectileData? projectileData, ProjectileOption? option, CancellationToken cancellationToken)
        {
            if (recasting)
            {
                try
                {
                    await canFire.First(v => v).ToUniTask(cancellationToken: cancellationToken);
                }catch(OperationCanceledException) {
                    return;
                }
            }
            setProjectileCore(projectileData, option);
        }

        void setTarget(ITransformProvider? target)
        {
            _target = target;
            _trajectoryVisualizer?.SetEndTarget(target?.Transform);
            setCanFire();
            if(_target!=null) {
                handleTriggerOn();
            } else {
                handleError();
            }
            handleError();
            onTargetChanged.OnNext(_target);
        }

        void fire()
        {
            if (!canFire.Value)
            {
                return;
            }
            if (_launcherHandler == null || _target == null)
            {
                throw new InvalidConditionException("fail to fire");
            }
            _launcherHandler.Fire(_launcherController.Mazzle, _target.Transform);
        }

        void handleTriggerOn()
        {
            if (triggerReq && canFire.Value)
            {
                if (_launcherHandler == null || _target == null)
                {
                    throw new InvalidConditionException("fail to fire");
                }
                _launcherHandler.TriggerOn(_launcherController.Mazzle, _target.Transform);
                triggered = true;
            }
        }

        void handleError()
        {
            // Debug.Log($"handleError. error:{!canFire.Value}");
            if (!canFire.Value)
            {
                if (_launcherHandler == null)
                {
                    throw new InvalidConditionException("fail to fire");
                }
                _launcherHandler.Error();
            }
        }

        void triggerOn()
        {
            if (triggerReq)
            {
                return;
            }
            triggerReq = true;
            if(!canFire.Value) {
                return;
            }
            if (_launcherHandler == null || _target == null)
            {
                throw new InvalidConditionException("fail to fire");
            }
            _launcherHandler.TriggerOn(_launcherController.Mazzle, _target.Transform);
            triggered = true;
        }

        void triggerOff()
        {
            if (!triggerReq)
            {
                return;
            }
            triggerReq = false;
            if (triggered)
            {
                if (_launcherHandler == null)
                {
                    throw new InvalidConditionException("fail to fire");
                }
                _launcherHandler.TriggerOff();
                triggered = false;
            }
        }

        void onBeforeLaunched()
        {
            changeRecastState(true);
        }

        void onAfterFire()
        {
            if(_projectileData==null) {
                throw new InvalidConditionException("ProjectileConfig was modified unexpectedly");
            }
            UniTask.Create(async () => {
                for (var i = 0; i < _projectileData.RecastTime; i += 100)
                {
                    await stepRecast(_projectileData.RecastTime, 100, i);
                }
                changeRecastState(false);
                handleTriggerOn();
            }).Forget();
        }

        #region ILauncher
        IProjectileData? ILauncher.ProjectileData { get => _projectileData; }
        void ILauncher.SetProjectile(IProjectileData? projectileObject, ProjectileOption? option) => setProjectile(projectileObject, option);
        UniTask ILauncher.SetProjectileAsync(IProjectileData? projectileObject, ProjectileOption? option, CancellationToken cancellationToken) 
            => setProjectileAsync(projectileObject, option, cancellationToken);
        ITransformProvider? ILauncher.Target { get => _target; }
        void ILauncher.SetTarget(ITransformProvider? target) => setTarget(target);

        IReadOnlyReactiveProperty<bool> ILauncher.CanFire { get => canFire; }
        void ILauncher.Fire() => fire();
        void ILauncher.TriggerOn() => triggerOn();
        void ILauncher.TriggerOff() => triggerOff();

        IObservable<IProjectileData?> ILauncher.OnProjectileChanged { get => onProjectileChanged; }
        IObservable<ITransformProvider?> ILauncher.OnTargetChanged { get => onTargetChanged; }
        IObservable<float> ILauncher.OnRecastTimeUpdated { get => onRecastTimeUpdated; }
        IObservable<IProjectile> ILauncher.OnFired { get => onFired; }
        #endregion

        #region ILauncherHandlerCallback
        void ILauncherHandlerCallback.OnShowTrajectory(bool v) => _trajectoryVisualizer?.Show(v);
        void ILauncherHandlerCallback.OnBeforeFire() => onBeforeLaunched();
        void ILauncherHandlerCallback.OnAfterFire() => onAfterFire();
        void ILauncherHandlerCallback.OnFired(IProjectile projectile) => onFired.OnNext(projectile);
        IProjectile ILauncherHandlerCallback.CreateProjectile(Vector3 position, string? name) => createProjectile(position, name);
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            onProjectileChanged.OnCompleted();
            onTargetChanged.OnCompleted();
            onRecastTimeUpdated.OnCompleted();
            _launcherHandler?.Dispose();
            canFire.Dispose();
            _disposable.Dispose();
        }
        #endregion

        public LauncherImpl(ILauncherController launcherController, ITimeManager? timeManager = null)
        {
            this._launcherController = launcherController;
            this._timeManager = timeManager;
            this._trajectoryVisualizer = ControllerBase.Find<ITrajectoryVisualizer>();
            this.initialize();
        }
    }
}
