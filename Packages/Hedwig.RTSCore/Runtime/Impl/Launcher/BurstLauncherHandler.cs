#nullable enable

using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class BurstLauncherHandler : ILauncherHandler
    {
        readonly ILauncherHandlerCallback _handlerEvent;
        readonly IProjectileData _projectileObject;

        CancellationTokenSource cts = new CancellationTokenSource();

        public void Fire(ITransform start, ITransform target)
        {
        }

        public void TriggerOn(ITransform start, ITransform target)
        {
            UniTask.Create(async () =>
            {
                _handlerEvent.OnBeforeFire();
                while (true)
                {
                    var projectile = _handlerEvent.CreateProjectile(start.Position, name: null);
                    projectile.Start(target);
                    _handlerEvent.OnFired(projectile);
                    try
                    {
                        await UniTask.Delay(100, cancellationToken: cts.Token);
                    }
                    catch
                    {
                        break;
                    }
                }
                _handlerEvent.OnAfterFire();
            }).Forget();
        }

        public void TriggerOff()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
        }

        public void Error()
        {
            Debug.Log("BurstLauncherHandler.Error. raise TriggerOff");
            TriggerOff();
        }

        public void Dispose()
        {
        }

        public BurstLauncherHandler(
            ILauncherHandlerCallback handlerEvent,
            IProjectileData projectileObject)
        {
            this._handlerEvent = handlerEvent;
            this._projectileObject = projectileObject;
        }
    }
}
