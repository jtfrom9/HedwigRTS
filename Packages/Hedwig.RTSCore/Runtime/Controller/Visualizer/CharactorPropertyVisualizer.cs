#nullable enable

using System;
using UnityEngine;
using TMPro;
using UniRx;


namespace Hedwig.RTSCore.Controller
{
    public class CharactorPropertyVisualizer : MonoBehaviour, ITargetVisualizer
    {
        ITransform _transform = new CachedTransform();
        TextMeshPro? _textMesh;
        bool _disposed = false;

        void Awake()
        {
            _transform.Initialize(transform);
            _textMesh = GetComponent<TextMeshPro>();
        }

        void OnDestroy()
        {
            _disposed = true;
        }

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            Destroy(gameObject);
        }

        void init(ITransform parent, IVisualProperty property)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.up * (property.DistanceToHead + 0.5f);

            _transform.OnPositionChanged.Subscribe(pos => {
                _transform.Raw?.LookAt(pos - (-Camera.main.transform.forward), Camera.main.transform.up);
            }).AddTo(this);
        }

        void ITargetVisualizer.Initialize(IVisualizerTarget target)
        {
            if(target.Transform==null || target.Charactor==null || target.VisualProperty==null) {
                return;
            }
            target.Charactor.Health.Subscribe(v => {
                if(_textMesh!=null)
                    _textMesh.text = $"{v}";
            }).AddTo(this);
            init(target.Transform, target.VisualProperty);
        }
    }
}
