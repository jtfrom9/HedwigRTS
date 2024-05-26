#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Impl
{
    public class GrenadeLauncherHandler : ILauncherHandler
    {
        readonly ILauncherHandlerCallback _handlerEvent;
        readonly IProjectileData _projectileData;

        ITransform? _start = null;
        ITransform? _target = null;

        public void Fire(ITransform start, ITransform target)
        {
        }

        public void TriggerOn(ITransform start, ITransform target)
        {
            _handlerEvent.OnShowTrajectory(true);
            _start = start;
            _target = target;
        }

        public void TriggerOff()
        {
            if(_start==null || _target==null)
                return;

            _handlerEvent.OnBeforeFire();
            var projectile = _handlerEvent.CreateProjectile(_start.Position, name: null);
            if (projectile == null)
            {
                Debug.LogError($"fiail to create {_projectileData.Name}");
                return;
            }
            projectile.Start(_target);
            _handlerEvent.OnFired(projectile);
            _handlerEvent.OnShowTrajectory(false);
            _handlerEvent.OnAfterFire();
        }

        public void Error()
        {
        }

        public void Dispose()
        {
        }

        public GrenadeLauncherHandler(
            ILauncherHandlerCallback handlerEvent,
            IProjectileData projectileData)
        {
            this._handlerEvent = handlerEvent;
            this._projectileData = projectileData;
        }
    }
}
