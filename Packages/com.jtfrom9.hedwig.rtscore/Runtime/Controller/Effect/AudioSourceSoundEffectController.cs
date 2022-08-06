#nullable enable

using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Controller
{
    public class AudioSourceSoundEffectController : ControllerBase, IDamageSoundEffect
    {
        float duration;
        int damage;
        AudioSource? audioSource;
        AudioClip? audioClip;

        bool _disposed = false;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        void OnDestroy()
        {
            audioSource?.Stop();
            _disposed = true;
        }

        async UniTask _play()
        {
            if(audioClip==null) return;
            audioSource?.PlayOneShot(audioClip);
            await UniTask.Delay((int)(audioClip.length * 1000));
        }

        #region IDamageSoundEffect
        public void Initialize(AudioClip clip, float volume)
        {
            if(audioSource==null) return;
            this.audioClip = clip;
            this.audioSource.volume = volume;
        }
        public UniTask Play() => _play();
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }
        #endregion
    }
}
