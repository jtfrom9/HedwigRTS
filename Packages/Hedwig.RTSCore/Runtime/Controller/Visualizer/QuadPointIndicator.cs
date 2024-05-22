#nullable enable

using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Assertions;

namespace Hedwig.RTSCore.Controller
{
    public class QuadPointIndicator: MonoBehaviour, IPointIndicator
    {
        [SerializeField] MeshRenderer? _renderer;

        ITransform _transform = new CachedTransform();
        bool _disposed = false;

        void Awake()
        {
            Assert.AreNotEqual(_renderer, null);

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

        ITransform ITransformProvider.Transform { get => _transform; }

        [ContextMenu("Init")]
        void IPointIndicator.Initialize()
        {
            transform.DOScale(Vector3.one * 1.5f, 1).SetLoops(-1, LoopType.Yoyo);
        }

        void IPointIndicator.Move(Vector3 pos)
        {
            transform.position = pos;
        }

        void IPointIndicator.SetColor(Color color)
        {
            if (_renderer != null)
            {
                _renderer.material.color = color;
            }
        }

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }
    }
}