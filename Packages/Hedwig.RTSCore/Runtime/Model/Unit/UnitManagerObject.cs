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
    [CreateAssetMenu(menuName = "Hedwig/Enemy/Manager", fileName = "EnemyManager")]
    public class UnitManagerObject : ScriptableObject, IEnemyAttackedEffectFactory, ITargetVisualizerFactory
    {
        #region IEnemyAttackedEffectFactory
        [SerializeField, InspectInline]
        List<DamageEffect> damageEffects = new List<DamageEffect>();

        [SerializeField, InspectInline]
        List<HitEffect> hitEffects = new List<HitEffect>();

        IEnumerable<IEffect?> createEffects(IUnit enemy, IHitObject? hitObject, DamageEvent e)
        {
            foreach (var damageEffect in damageEffects)
            {
                yield return damageEffect.Create(enemy.Controller, e.ActualDamage);
            }
            foreach (var hitEffect in hitEffects)
            {
                yield return hitEffect.Create(enemy.Controller,
                    hitObject?.Position ?? enemy.Controller.Transform.Position,
                    Vector3.zero);
            }
        }

        IEffect[] IEnemyAttackedEffectFactory.CreateAttackedEffects(IUnit enemy, IHitObject? hitObject, in DamageEvent e)
            => createEffects(enemy, hitObject, e)
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
        public static void SetupEnemyManager(this IContainerBuilder builder, UnitManagerObject? UnitManagerObject)
        {
            if (UnitManagerObject == null)
            {
                throw new ArgumentNullException("UnitManagerObject is null");
            }
            builder.RegisterInstance<UnitManagerObject>(UnitManagerObject).AsImplementedInterfaces();
            builder.Register<IUnitManager, UnitManagerImpl>(Lifetime.Singleton);
        }
    }
}
