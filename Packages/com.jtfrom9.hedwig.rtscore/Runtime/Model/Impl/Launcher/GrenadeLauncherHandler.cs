#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public class GrenadeLauncherHandler : ILauncherHandler
    {
        ILauncherHandlerEvent handlerEvent;
        ProjectileObject projectileObject;

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
            var projectile = projectileObject.Create(_start.Position);
            if (projectile == null)
            {
                Debug.LogError($"fiail to create {projectileObject.name}");
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
            ProjectileObject projectileObject)
        {
            this.handlerEvent = handlerEvent;
            this.projectileObject = projectileObject;
        }
    }
}
