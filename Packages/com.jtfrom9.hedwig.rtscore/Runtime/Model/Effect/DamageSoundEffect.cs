#nullable enable

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/DamageSound", fileName = "DamageSoundEffect")]
    public class DamageSoundEffect : DamageEffect
    {
        [SerializeField]
        AudioClip? audioClip;

        [SerializeField, Range(0f, 1f)]
        float volume;

        public override IEffect? Create(ITransformProvider parent, int damage)
        {
            if (prefab == null) return null;
            if (audioClip == null) return null;
            var effect = Instantiate(prefab).GetComponent<IDamageSoundEffect>();
            effect?.Initialize(audioClip, volume);
            return effect;
        }
    }
}