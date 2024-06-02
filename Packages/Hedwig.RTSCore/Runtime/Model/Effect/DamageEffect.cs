#nullable enable

using System;
using UnityEngine;
using UnityEngine.Search;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Damage", fileName = "DamageEffect")]
    public class DamageEffect : ScriptableObject, IDamageEffectFactory
    {
        [SerializeReference, SubclassSelector]
        [Required]
        IDamageEffectFactory Factory;

        public IEffect Create(ITransformProvider parent, int damage)
            => Factory.Create(parent, damage);
    }

    public interface IDamageEffectFactory
    {
        IEffect Create(ITransformProvider parent, int damage);
    }

    [Serializable]
    public class PrefabDamageEffect : IDamageEffectFactory
    {
        [SerializeField, SearchContext("t:prefab effect")]
        [SerializableRequired]
        GameObject? prefab;

        [SerializeField]
        [SerializableRequired]
        DamageEffectParameter? damageEffectParameter;

        public IEffect Create(ITransformProvider parent, int damage)
        {
            if (!GameObject.Instantiate(prefab!).TryGetComponent<IDamageEffect>(out var effect))
            {
                throw new InvalidConditionException("No IDamageEffect");
            }
            effect.Initialize(parent, damageEffectParameter!, damage);
            return effect;
        }
    }

    [Serializable]
    public class SoundDamageEffect : IDamageEffectFactory
    {
        [SerializeField, SearchContext("t:prefab effect")]
        [SerializableRequired]
        GameObject? prefab;

        [SerializeField]
        [SerializableRequired]
        AudioClip? audioClip;

        [SerializeField, Range(0f, 1f)]
        float volume;

        public IEffect Create(ITransformProvider parent, int damage)
        {
            if (!GameObject.Instantiate(prefab!).TryGetComponent<IDamageSoundEffect>(out var effect))
            {
                throw new InvalidConditionException("No IDamageSoundEffect");
            }
            effect.Initialize(audioClip!, volume);
            return effect;
        }
    }
}