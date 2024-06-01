#nullable enable

using UnityEngine;
using UnityEngine.Search;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Hit", fileName = "HitEffect")]
    public class HitEffect : ScriptableObject
    {
        [SerializeField, SearchContext("t:prefab effect")]
        [Required]
        GameObject? prefab;

        public IEffect Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            if (!Instantiate(prefab!).TryGetComponent<IHitEffect>(out var effect))
            {
                throw new InvalidConditionException("No IHitEffect");
            }
            effect.Initialize(parent, position, direction);
            return effect;
        }
    }
}