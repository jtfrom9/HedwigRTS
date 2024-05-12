#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class EnvironmentImpl : IEnvironment, IEnvironmentEvent
    {
        IEnvironmentController environmentController;
        IEnvironmentEffectFactory effectFactory;

        void IEnvironmentEvent.OnHit(IHitObject hitObject)
        {
            Debug.Log($"{this}: OnHit");
            var effects = effectFactory.CreateEffects(this, hitObject.position, -hitObject.direction);
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

        public EnvironmentImpl(IEnvironmentEffectFactory effectFactory, IEnvironmentController environmentController)
        {
            this.effectFactory = effectFactory;
            this.environmentController = environmentController;
        }
    }
}