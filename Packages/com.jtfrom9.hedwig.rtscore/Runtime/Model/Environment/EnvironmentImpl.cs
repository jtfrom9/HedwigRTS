#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public class EnvironmentImpl : IEnvironment, IEnvironmentEvent
    {
        IEnvironmentController environmentController;
        EnvironmentObject environmentObject;

        void IEnvironmentEvent.OnHit(IHitObject hitObject)
        {
            Debug.Log($"{this}: OnHit");
            var effects = environmentObject.environmentEffects?.CreateEffects(this,
                hitObject.position,
                -hitObject.direction) ?? Array.Empty<IEffect>();
            foreach (var effect in effects)
            {
                effect.PlayAndDispose().Forget();
            }
        }

        IEnvironmentController IEnvironment.controller { get => environmentController; }

        public override string ToString()
        {
            return $"{environmentController.name}.Impl";
        }

        public EnvironmentImpl(EnvironmentObject environmentObject, IEnvironmentController environmentController)
        {
            this.environmentObject = environmentObject;
            this.environmentController = environmentController;
        }
    }
}