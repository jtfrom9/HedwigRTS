#nullable enable

using UnityEngine;
using UnityEngine.Search;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Damage", fileName = "DamageEffect")]
    public class DamageEffect : ScriptableObject
    {
        [SerializeField, SearchContext("t:prefab effect")]
        protected GameObject? prefab;

        [SerializeField]
        DamageEffectParameter? damageEffectParameter;

        public virtual IEffect? Create(ITransformProvider parent, int damage)
        {
            if (prefab == null) return null;
            if (damageEffectParameter == null) return null;
            var effect = Instantiate(prefab).GetComponent<IDamageEffect>();
            effect?.Initialize(parent, damageEffectParameter, damage);
            return effect;
        }
    }
}