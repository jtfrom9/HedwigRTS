#nullable enable

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Enemy/Effects", fileName = "EnemyEffects")]
    public class EnemyEffectsObject : ScriptableObject, IEnemyAttackedEffectFactory
    {
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
    }
}