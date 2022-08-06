#nullable enable

using System;
using UnityEngine;
using DG.Tweening;

namespace Hedwig.RTSCore.Controller
{
    public class CircleVisualizer : MonoBehaviour, IFreeCursorVisualizer
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

        ITransform ITransformProvider.transform { get => _transform; }

        [ContextMenu("Init")]
        void IFreeCursorVisualizer.Initialize()
        {
            transform.DOScale(Vector3.one * 1.5f, 1).SetLoops(-1, LoopType.Yoyo);
        }

        void IFreeCursorVisualizer.Move(Vector3 pos)
        {
            transform.position = pos;
        }

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }
    }
}