#nullable enable

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore.ThirdParty
{
    public class HitEffectController : MonoBehaviour, IHitEffect
    {
        [SerializeField]
        ParticleSystem? _particleSystem;

        EffectCompleteHandler? completeHandler;

        class EffectCompleteHandler : MonoBehaviour {
            void OnParticleSystemStopped()
            {
                onComplete.OnNext(Unit.Default);
            }
            public Subject<Unit> onComplete = new Subject<Unit>();
        }

        void Awake()
        {
            completeHandler = _particleSystem?.gameObject.AddComponent<EffectCompleteHandler>();
        }

        #region IDamageEffect
        public void Initialize(ITransformProvider parent, Vector3 position, Vector3 normal)
        {
            transform.position = position;
            transform.SetParent(parent.transform, true);
        }

        public UniTask Play()
        {
            _particleSystem?.Play();
            return completeHandler!.onComplete.ToUniTask(true);
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            Destroy(gameObject);
        }
        #endregion

    }
}
