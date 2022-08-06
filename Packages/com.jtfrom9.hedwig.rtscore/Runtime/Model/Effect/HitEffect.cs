#nullable enable

using UnityEngine;
using UnityEngine.Search;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Hit", fileName = "HitEffect")]
    public class HitEffect : ScriptableObject
    {
        [SerializeField, SearchContext("t:prefab effect")]
        GameObject? prefab;

        public IEffect? Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            if (prefab == null) return null;
            var effect = Instantiate(prefab).GetComponent<IHitEffect>();
            effect?.Initialize(parent, position, direction);
            return effect;
        }
    }
}