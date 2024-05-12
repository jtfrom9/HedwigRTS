#nullable enable

using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class BurstLauncherHandler : ILauncherHandler
    {
        ILauncherHandlerEvent handlerEvent;
        IProjectileData projectileObject;

        CancellationTokenSource cts = new CancellationTokenSource();

        public void Fire(ITransform start, ITransform target)
        {
        }

        public void TriggerOn(ITransform start, ITransform target)
        {
            UniTask.Create(async () =>
            {
                handlerEvent.OnBeforeFire();
                while (true)
                {
                    var projectile = projectileObject.Factory.Create(start.Position);
                    if (projectile == null)
                    {
                        Debug.LogError($"fail to create {projectileObject.Name}");
                        break;
                    }
                    projectile.Start(target);
                    handlerEvent.OnFired(projectile);
                    try
                    {
                        await UniTask.Delay(100, cancellationToken: cts.Token);
                    }
                    catch
                    {
                        break;
                    }
                }
                handlerEvent.OnAfterFire();
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
            ILauncherHandlerEvent handlerEvent,
            IProjectileData projectileObject)
        {
            this.handlerEvent = handlerEvent;
            this.projectileObject = projectileObject;
        }
    }
}
