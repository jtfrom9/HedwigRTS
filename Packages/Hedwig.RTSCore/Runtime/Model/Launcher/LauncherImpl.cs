#nullable enable

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore.Model
{
    public class LauncherImpl : ILauncher, ILauncherHandlerEvent
    {
        IProjectileData? _projectileData;
        ITransformProvider? _target;
        CompositeDisposable disposable = new CompositeDisposable();

        bool initialized = false;
        bool recasting = false;
        bool triggerReq = false;
        bool triggered = false;

        ReactiveProperty<bool> canFire = new ReactiveProperty<bool>();
        Subject<IProjectileData?> onProjectileChanged = new Subject<IProjectileData?>();
        Subject<ITransformProvider?> onTargetChanged = new Subject<ITransformProvider?>();
        Subject<float> onRecastTimeUpdated = new Subject<float>();
        Subject<IProjectile> onFired = new Subject<IProjectile>();

        // injected
        ILauncherController launcherController;

        // find
        ITrajectoryVisualizer? trajectoryVisualizer;

        ILauncherHandler? launcherHandler;

        void initialize()
        {
            if (trajectoryVisualizer != null)
            {
                trajectoryVisualizer.SetStartTarget(launcherController.mazzle);
            }
            launcherController.Initialize(this);
            initialized = true;
        }

        void setCanFire()
        {
            var v = _projectileData != null &&
                _target != null &&
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

        void setProjectileCore(IProjectileData? projectileData, ProjectileOption? option)
        {
            this._projectileData = projectileData;
            this.trajectoryVisualizer?.SetProjectile(projectileData);

            // reset laouncher handler
            this.launcherHandler?.Dispose();
            this.launcherHandler = null;

            var projectileObject = projectileData as ProjectileObject; // FIXME
            if (projectileData!=null && projectileObject != null)
            {
                switch (projectileData.Type)
                {
                    case ProjectileType.Fire:
                        this.launcherHandler = new ShotLauncherHandler(this, projectileObject, option);
                        break;
                    case ProjectileType.Burst:
                        this.launcherHandler = new BurstLauncherHandler(this, projectileObject);
                        break;
                    case ProjectileType.Grenade:
                        this.launcherHandler = new GrenadeLauncherHandler(this, projectileObject);
                        break;
                }
            }
            setCanFire();
            handleError();
            onProjectileChanged.OnNext(projectileData);
        }

        void setProjectile(IProjectileData? projectileData, ProjectileOption? option)
        {
            if (!initialized)
            {
                throw new InvalidConditionException("LauncherManager is not Initalized");
            }
            if (recasting) {
                throw new InvalidConditionException("LauncherManager is Recasting");
            }
            setProjectileCore(projectileData, option);
        }

        async UniTask setProjectileAsync(IProjectileData? projectileObject, ProjectileOption? option, CancellationToken cancellationToken)
        {
            if (!initialized)
            {
                throw new InvalidConditionException("LauncherManager is not Initalized");
            }
            if (recasting)
            {
                try
                {
                    await canFire.First(v => v).ToUniTask(cancellationToken: cancellationToken);
                }catch(OperationCanceledException) {
                    return;
                }
            }
            setProjectileCore(projectileObject, option);
        }

        void setTarget(ITransformProvider? target)
        {
            if (!initialized)
            {
                throw new InvalidConditionException("LauncherManager is not Initalized");
            }
            _target = target;
            trajectoryVisualizer?.SetEndTarget(target?.transform);
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
            if (!initialized)
                throw new InvalidConditionException("LauncherManager is not Initalized");
            if (_target == null)
                return;
            if (launcherHandler == null)
                return;
            if (_projectileData == null)
                return;
            if (!canFire.Value)
                return;
            launcherHandler.Fire(launcherController.mazzle, _target.transform);
        }

        void handleTriggerOn()
        {
            // Debug.Log($"handleTrigerOn. {_target}, req:{triggerReq}");
            if (_target == null)
                return;
            if (launcherHandler == null)
                return;
            if (triggerReq && canFire.Value)
            {
                launcherHandler.TriggerOn(launcherController.mazzle, _target.transform);
                triggered = true;
            }
        }

        void handleError()
        {
            // Debug.Log($"handleError. error:{!canFire.Value}");
            if (launcherHandler == null)
                return;
            if (!canFire.Value) 
            {
                launcherHandler.Error();
            }
        }

        void triggerOn()
        {
            if (!initialized)
                throw new InvalidConditionException("LauncherManager is not Initalized");
            if (_target == null)
                return;
            if (launcherHandler == null)
                return;
            if(triggerReq)
                return;
            triggerReq = true;
            if(!canFire.Value) {
                return;
            }
            launcherHandler.TriggerOn(launcherController.mazzle, _target.transform);
            triggered = true;
        }

        void triggerOff()
        {
            if (!initialized)
                throw new InvalidConditionException("LauncherManager is not Initalized");
            if (launcherHandler == null)
                return;
            if(!triggerReq)
                return;
            triggerReq = false;
            if (triggered)
            {
                launcherHandler.TriggerOff();
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
        void ILauncher.Initialize() => initialize();
        IProjectileData? ILauncher.projectileObject { get => _projectileData; }
        void ILauncher.SetProjectile(IProjectileData? projectileObject, ProjectileOption? option) => setProjectile(projectileObject, option);
        UniTask ILauncher.SetProjectileAsync(IProjectileData? projectileObject, ProjectileOption? option, CancellationToken cancellationToken) 
            => setProjectileAsync(projectileObject, option, cancellationToken);
        ITransformProvider? ILauncher.target { get => _target; }
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

        #region ILauncherManager
        void ILauncherHandlerEvent.OnShowTrajectory(bool v) => trajectoryVisualizer?.Show(v);
        void ILauncherHandlerEvent.OnBeforeFire() => onBeforeLaunched();
        void ILauncherHandlerEvent.OnAfterFire() => onAfterFire();
        void ILauncherHandlerEvent.OnFired(IProjectile projectile) => onFired.OnNext(projectile);
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            onProjectileChanged.OnCompleted();
            onTargetChanged.OnCompleted();
            onRecastTimeUpdated.OnCompleted();
            launcherHandler?.Dispose();
            canFire.Dispose();
            disposable.Dispose();
        }
        #endregion

        public LauncherImpl(ILauncherController launcherController)
        {
            this.launcherController = launcherController;
            this.trajectoryVisualizer = ControllerBase.Find<ITrajectoryVisualizer>();
        }
    }
}
