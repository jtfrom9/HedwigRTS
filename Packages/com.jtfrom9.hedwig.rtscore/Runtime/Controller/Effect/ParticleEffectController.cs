#nullable enable

using System;
using UnityEngine;
using UnityEngine.Assertions;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Controller
{
    public class ParticleEffectController : ControllerBase, IHitEffect
    {
        bool _disposed = false;
        ParticleSystem? _particleSystem = null;
        UniTaskCompletionSource? uts = null;

        void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            Assert.IsNotNull(_particleSystem);
        }

        void OnDestroy()
        {
            _disposed = true;
        }

        void OnParticleSystemStopped()
        {
            // this._task?.Com
            this.uts?.TrySetResult();
        }

        #region IEffect
        UniTask IEffect.Play()
        {
            _particleSystem?.Play();
            this.uts = new UniTaskCompletionSource();
            return this.uts.Task;
        }
        #endregion

        #region IHitEffect
        void IHitEffect.Initialize(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            transform.position = position;
            transform.LookAt(position + direction);
            transform.SetParent(parent.transform, worldPositionStays: true);
        }
        #endregion

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }
    }
}