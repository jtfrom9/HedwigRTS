#nullable enable

using System;
using UnityEngine;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/DamageSound", fileName = "DamageSoundEffect")]
    public class DamageSoundEffect : DamageEffect
    {
        [SerializeField]
        [Required]
        AudioClip? audioClip;

        [SerializeField, Range(0f, 1f)]
        float volume;

        public override IEffect Create(ITransformProvider parent, int damage)
        {
            if (!Instantiate(prefab!).TryGetComponent<IDamageSoundEffect>(out var effect))
            {
                throw new InvalidConditionException("No IDamageSoundEffect");
            }
            effect.Initialize(audioClip!, volume);
            return effect;
        }
    }
}