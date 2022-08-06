#nullable enable

using System;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Controller
{
    public class DamageTextEffectController : MonoBehaviour, IDamageEffect
    {
        float duration;
        int damage;
        TextMeshPro? tmp;
        ITransform? cameraTransform;

        bool _disposed = false;

        void Awake() {
            tmp = GetComponentInChildren<TextMeshPro>();
            cameraTransform = CameraTransform.Find();
        }

        void OnDestroy() {
            if(DOTween.IsTweening(transform)) {
                transform.DOKill();
            }
            _disposed = true;
        }

        UniTask _play()
        {
            var token = this.GetCancellationTokenOnDestroy();

            // t1 = transform.DOLocalMoveY(1, duration).ToUniTask(cancellationToken: token);
            var t1 = transform.DOLocalMoveY(1, duration).OnUpdate(() =>
            {
                if(cameraTransform!=null)
                    transform.LookAt(cameraTransform.Position);
            }).ToUniTask(cancellationToken: token);

            var t2 = DOTween.To(
                () => 255,
                (value) =>
                {
                    tmp!.text = $"<alpha=#{(int)value:X02}>{damage}";
                },
                0, duration).ToUniTask(cancellationToken: token);
            return UniTask.WhenAll(t1, t2);
        }

        #region IDamageEffect
        public void Initialize(ITransformProvider parent, DamageEffectParameter param, int damage)
        {
            this.duration = param.duration;
            this.damage = damage;
            transform.SetParent(parent.transform, false);
        }
        public UniTask Play() => _play();
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            if(_disposed) return;
            Destroy(gameObject);
        }
        #endregion
    }
}
