#nullable enable

using System;
using UnityEngine;
using UnityEngine.Search;

using Cysharp.Threading.Tasks;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Effect/Hit", fileName = "HitEffect")]
    public class HitEffect : ScriptableObject, IHitEffectFactory
    {
        [SerializeReference, SubclassSelector]
        IHitEffectFactory Factory;

        public IEffect Create(ITransformProvider parent, Vector3 position, Vector3 direction)
            => Factory.Create(parent, position, direction);
    }

    public interface IHitEffectFactory
    {
        IEffect Create(ITransformProvider parent, Vector3 position, Vector3 direction);
    }

    [Serializable]
    public class DelayHitEffect : IHitEffectFactory
    {
        [SerializeField]
        int msec = 0;

        class DelayEffectImpl: IEffect
        {
            public int msec;
            UniTask IEffect.Play() {
                return UniTask.Delay(msec);
            }
            void IDisposable.Dispose()
            {}
        }

        IEffect IHitEffectFactory.Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            return new DelayEffectImpl() { msec = msec };
        }
    }

    [Serializable]
    public class PrefabHitEffect: IHitEffectFactory
    {
        [SerializeField, SearchContext("t:prefab effect")]
        [SerializableRequired]
        public GameObject? prefab;

        IEffect IHitEffectFactory.Create(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            if (!GameObject.Instantiate(prefab!).TryGetComponent<IHitEffect>(out var effect))
            {
                throw new InvalidConditionException("No IHitEffect");
            }
            effect.Initialize(parent, position, direction);
            return effect;
        }
    }
}