#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;
using VContainer;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Enemy/Manager", fileName = "EnemyManager")]
    public class EnemyManagerObject : ScriptableObject, IEnemyAttackedEffectFactory
    {
        [SerializeField, InspectInline]
        EnemyEffectsObject? _effects;

        IEffect[] IEnemyAttackedEffectFactory.CreateAttackedEffects(IEnemy enemy, IHitObject? hitObject, in DamageEvent e)
        {
            return _effects?.CreateAttackedEffects(enemy, hitObject, e) ?? new IEffect[] { };
        }
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
