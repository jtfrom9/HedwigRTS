#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;
using VContainer;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Enemy/Manager", fileName = "EnemyManager")]
    public class EnemyManagerObject : ScriptableObject, IEnemyAttackedEffectFactory, ITargetVisualizerFactory
    {
        #region IEnemyAttackedEffectFactory
        [SerializeField, InspectInline]
        List<DamageEffect> damageEffects = new List<DamageEffect>();

        [SerializeField, InspectInline]
        List<HitEffect> hitEffects = new List<HitEffect>();

        IEnumerable<IEffect?> createEffects(IEnemy enemy, IHitObject? hitObject, DamageEvent e)
        {
            foreach (var damageEffect in damageEffects)
            {
                yield return damageEffect.Create(enemy.controller, e.actualDamage);
            }
            foreach (var hitEffect in hitEffects)
            {
                yield return hitEffect.Create(enemy.controller,
                    hitObject?.position ?? enemy.controller.transform.Position,
                    Vector3.zero);
            }
        }

        IEffect[] IEnemyAttackedEffectFactory.CreateAttackedEffects(IEnemy enemy, IHitObject? hitObject, in DamageEvent e)
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

    public static class EnemyManagerObjectDIExtension
    {
        public static void SetupEnemyManager(this IContainerBuilder builder, EnemyManagerObject? enemyManagerObject)
        {
            if (enemyManagerObject == null)
            {
                throw new ArgumentNullException("enemyManagerObject is null");
            }
            builder.RegisterInstance<EnemyManagerObject>(enemyManagerObject).AsImplementedInterfaces();
            builder.Register<IEnemyManager, EnemyManagerImpl>(Lifetime.Singleton);
        }
    }
}
