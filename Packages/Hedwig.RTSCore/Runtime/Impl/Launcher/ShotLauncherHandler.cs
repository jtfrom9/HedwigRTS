#nullable enable

using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore.Impl
{
    public class ShotLauncherHandler: ILauncherHandler
    {
        readonly ILauncherHandlerCallback _handlerEvent;
        readonly IProjectileData _projectileData;
        readonly ProjectileOption? _option;

        public void Fire(ITransform start, ITransform target)
        {
            UniTask.Create(async () =>
            {
                _handlerEvent.OnBeforeFire();
                for (var i = 0; i < _projectileData.SuccessionCount; i++)
                {
                    var cts = new CancellationTokenSource();
                    var projectile = _handlerEvent.CreateProjectile(start.Position, name: null);
                    var disposable = projectile.OnDestroy.Subscribe(_ =>
                    {
                        cts.Cancel();
                    });
                    projectile.Start(target, in _option);
                    _handlerEvent.OnFired(projectile);

                    if (_projectileData.SuccessionCount > 1)
                    {
                        try
                        {
                            await UniTask.Delay(_projectileData.SuccessionInterval, cancellationToken: cts.Token);
                        }catch(OperationCanceledException) {
                            break;
                        }finally {
                            disposable.Dispose();
                            cts.Dispose();
                        }
                    }
                }
                _handlerEvent.OnAfterFire();
            }).Forget();
        }

        public void TriggerOn(ITransform start, ITransform target)
        {
            Debug.Log($"StartFire: {_projectileData.Chargable}");
            if (_projectileData.Chargable)
            {
                _handlerEvent.OnShowTrajectory(true);
            }
        }

        public void TriggerOff()
        {
            Debug.Log("EndFire");
            if (_projectileData.Chargable)
            {
                _handlerEvent.OnShowTrajectory(false);
            }
        }

        public void Error()
        {
        }

        public void Dispose()
        {
        }

        public ShotLauncherHandler(
            ILauncherHandlerCallback handlerEvent,
            IProjectileData projectileData,
            ProjectileOption? option)
        {
            this._handlerEvent = handlerEvent;
            this._projectileData = projectileData;
            this._option = option;
        }
    }
}
