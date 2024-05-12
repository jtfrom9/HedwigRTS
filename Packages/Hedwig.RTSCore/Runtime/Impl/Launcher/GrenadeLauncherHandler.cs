#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Impl
{
    public class GrenadeLauncherHandler : ILauncherHandler
    {
        ILauncherHandlerEvent handlerEvent;
        IProjectileData projectileData;

        ITransform? _start = null;
        ITransform? _target = null;

        public void Fire(ITransform start, ITransform target)
        {
        }

        public void TriggerOn(ITransform start, ITransform target)
        {
            handlerEvent.OnShowTrajectory(true);
            _start = start;
            _target = target;
        }

        public void TriggerOff()
        {
            if(_start==null || _target==null)
                return;

            handlerEvent.OnBeforeFire();
            var projectile = projectileData.Factory.Create(_start.Position);
            if (projectile == null)
            {
                Debug.LogError($"fiail to create {projectileData.Name}");
                return;
            }
            projectile.Start(_target);
            handlerEvent.OnFired(projectile);
            handlerEvent.OnShowTrajectory(false);
            handlerEvent.OnAfterFire();
        }

        public void Error()
        {
        }

        public void Dispose()
        {
        }

        public GrenadeLauncherHandler(
            ILauncherHandlerEvent handlerEvent,
            IProjectileData projectileData)
        {
            this.handlerEvent = handlerEvent;
            this.projectileData = projectileData;
        }
    }
}
