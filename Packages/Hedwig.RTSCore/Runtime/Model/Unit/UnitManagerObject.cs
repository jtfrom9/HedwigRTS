#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;
using UniRx;
using VContainer;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/UnitManager", fileName = "UnitManager")]
    public class UnitManagerObject : ScriptableObject, IUnitAttackedEffectFactory, ITargetVisualizerFactory
    {
        [SerializeField, InspectInline]
        List<DamageEffect> damageEffects;

        [SerializeField, InspectInline]
        List<HitEffect> hitEffects;

        [SerializeField, InspectInline]
        List<HitEffect> deathEffects;

        IEnumerable<IEffect> CreateAttackedEffects(IUnit unit, IHitObject? hitObject, DamageEvent e)
        {
            foreach (var damageEffect in damageEffects)
            {
                yield return damageEffect.Create(unit.Controller, e.ActualDamage);
            }
            foreach (var hitEffect in hitEffects)
            {
                yield return hitEffect.Create(unit.Controller,
                    hitObject?.Position ?? unit.Controller.Transform.Position,
                    Vector3.zero);
            }
        }

        IEnumerable<IEffect> CreateDeathEffects(IUnit unit, IHitObject? hitObject)
        {
            foreach (var deathEffect in deathEffects)
            {
                yield return deathEffect.Create(unit.Controller, unit.Transform.Position, hitObject?.Direction ?? Vector3.zero);
            }
        }

        #region IEnemyAttackedEffectFactory
        IEffect[] IUnitAttackedEffectFactory.CreateAttackedEffects(IUnit unit, IHitObject? hitObject, in DamageEvent e)
            => CreateAttackedEffects(unit, hitObject, e)
                .ToArray();
        IEffect[] IUnitAttackedEffectFactory.CreateDeathEffects(IUnit unit, IHitObject? hitObject)
            => CreateDeathEffects(unit, hitObject).ToArray();
        #endregion

        [SerializeField, InspectInline]
        List<TargetVisualizerObject> targetVisualizers = new List<TargetVisualizerObject>();

        IEnumerable<ITargetVisualizer> createVisualizers(IVisualizerTarget target)
        {
            foreach (var vobj in targetVisualizers)
            {
                yield return vobj.Create(target);
            }
        }

        #region ITargetVisualizerFactory
        IEnumerable<ITargetVisualizer> ITargetVisualizerFactory.CreateTargetVisualizers(IVisualizerTarget target)
            => createVisualizers(target);
        #endregion
    }

    public static class UnitManagerObjectDIExtension
    {
        public static void RegisterUnitManager(this IContainerBuilder builder, UnitManagerObject? unitManagerObject)
        {
            if (unitManagerObject == null)
            {
                throw new ArgumentNullException("UnitManagerObject is null");
            }
            builder.RegisterInstance<UnitManagerObject>(unitManagerObject).AsImplementedInterfaces();
            builder.Register<UnitManagerImpl>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
