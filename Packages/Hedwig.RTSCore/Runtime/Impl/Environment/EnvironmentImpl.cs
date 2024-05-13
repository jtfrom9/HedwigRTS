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
            var effects = effectFactory.CreateEffects(this, hitObject.Position, -hitObject.Direction);
            foreach (var effect in effects)
            {
                effect.PlayAndDispose().Forget();
            }
        }

        IEnvironmentController IEnvironment.Controller { get => environmentController; }

        public override string ToString()
        {
            return $"{environmentController.Name}.Impl";
        }

        public EnvironmentImpl(IEnvironmentEffectFactory effectFactory, IEnvironmentController environmentController)
        {
            this.effectFactory = effectFactory;
            this.environmentController = environmentController;
        }
    }
}