#nullable enable

using System;
using UnityEngine;

using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Hedwig.RTSCore.Controller
{
    public class SphereHitEffectController : MonoBehaviour, IHitEffect
    {
        bool _disposed = false;

        void OnDestroy()
        {
            if (DOTween.IsTweening(transform))
            {
                transform.DOKill();
            }
            _disposed = true;
        }


        #region IEffect
        UniTask IEffect.Play()
        {
            return transform.DOShakeScale(1f, 2f, 20, 1, true).ToUniTask();
        }
        #endregion

        #region IHitEffect
        void IHitEffect.Initialize(ITransformProvider parent, Vector3 position, Vector3 direction)
        {
            transform.position = position;
            transform.SetParent(parent.transform);
        }
        #endregion

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }
    }
}