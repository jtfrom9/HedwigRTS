#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;
using VContainer;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/UnitManager", fileName = "UnitManager")]
    public class UnitManagerObject : ScriptableObject, IUnitAttackedEffectFactory, ITargetVisualizerFactory
    {
        #region IEnemyAttackedEffectFactory
        [SerializeField, InspectInline]
        List<DamageEffect> damageEffects = new List<DamageEffect>();

        [SerializeField, InspectInline]
        List<HitEffect> hitEffects = new List<HitEffect>();

        IEnumerable<IEffect?> createEffects(IUnit unit, IHitObject? hitObject, DamageEvent e)
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

        IEffect[] IUnitAttackedEffectFactory.CreateAttackedEffects(IUnit unit, IHitObject? hitObject, in DamageEvent e)
            => createEffects(unit, hitObject, e)
                .WhereNotNull()
                .ToArray();
        #endregion

        #region ITargetVisualizerFactory
        [SerializeField, InspectInline]
        List<TargetVisualizerObject> targetVisualizers = new List<TargetVisualizerObject>();

        IEnumerable<ITargetVisualizer?> createVisualizers(IVisualizerTarget target)
        {
            foreach (var vobj in targetVisualizers)
            {
                yield return vobj.Create(target);
            }
        }

        IEnumerable<ITargetVisualizer> ITargetVisualizerFactory.CreateTargetVisualizers(IVisualizerTarget target)
            => createVisualizers(target).WhereNotNull();
        #endregion
    }

    public static class UnitManagerObjectDIExtension
    {
        public static void SetupUnitManager(this IContainerBuilder builder, UnitManagerObject? unitManagerObject)
        {
            if (unitManagerObject == null)
            {
                throw new ArgumentNullException("UnitManagerObject is null");
            }
            builder.RegisterInstance<UnitManagerObject>(unitManagerObject).AsImplementedInterfaces();
            builder.Register<IUnitManager, UnitManagerImpl>(Lifetime.Singleton);
        }
    }
}
