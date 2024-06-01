#nullable enable

using UnityEngine;
using UnityEngine.Search;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Damage", fileName = "DamageEffect")]
    public class DamageEffect : ScriptableObject
    {
        [SerializeField, SearchContext("t:prefab effect")]
        [Required]
        public GameObject? prefab;

        [SerializeField]
        DamageEffectParameter? damageEffectParameter;

        public virtual IEffect Create(ITransformProvider parent, int damage)
        {
            if (!Instantiate(prefab!).TryGetComponent<IDamageEffect>(out var effect))
            {
                throw new InvalidConditionException("No IDamageEffect");
            }
            effect.Initialize(parent, damageEffectParameter!, damage);
            return effect;
        }
    }
}