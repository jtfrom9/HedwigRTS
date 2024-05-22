#nullable enable

using System;
using UnityEngine;
using DG.Tweening;
using UniRx;

namespace Hedwig.RTSCore.Controller
{
    public class QuadTargetVisualizer : MonoBehaviour, ITargetVisualizer
    {
        ITransform _transform = new CachedTransform();
        bool _disposed = false;

        void Awake()
        {
            _transform.Initialize(transform);
        }

        void OnDestroy()
        {
            _disposed = true;
            if (DOTween.IsTweening(transform))
            {
                transform.DOKill();
            }
        }

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }

        void init(ITransform parent, IVisualProperty property)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.down * property.DistanceToGround;
            transform.DOScale(Vector3.one * 1.5f, 1).SetLoops(-1, LoopType.Yoyo);
            gameObject.SetActive(false);
        }

        void ITargetVisualizer.Initialize(IVisualizerTarget target)
        {
            var visualProp = target.VisualProperty;
            var selectable = target.Selectable;
            if (visualProp == null || selectable == null || target.Transform == null)
            {
                return;
            }
            var gameObject = this.gameObject;
            selectable.Selected.Subscribe(v =>
            {
                gameObject.SetActive(v);
            }).AddTo(this);
            init(target.Transform, visualProp);
        }
    }
}