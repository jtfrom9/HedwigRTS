#nullable enable

using System;
using UnityEngine;
using UniRx;
using System.Collections.Generic;

namespace Hedwig.RTSCore.Controller
{
    [RequireComponent(typeof(LineRenderer))]
    public class TargetingLineIndicator : MonoBehaviour, ITargetVisualizer
    {
        ITransform _transform = new CachedTransform();
        LineRenderer? _lineRenderer;
        bool _disposed = false;

        void Awake()
        {
            _transform.Initialize(transform);
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
            _lineRenderer.startWidth = 0.1f;
            _lineRenderer.endWidth = 0.1f;
            _lineRenderer.material.color = Color.white;
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

        Vector3 makeBezierCurvePoint(Vector3 start, Vector3 end, Vector3 control, float t)
        {
            Vector3 Q0 = Vector3.Lerp(start, control, t);
            Vector3 Q1 = Vector3.Lerp(control, end, t);
            Vector3 Q2 = Vector3.Lerp(Q0, Q1, t);
            return Q2;
        }

        void updateLine(Vector3 p1, Vector3 p2)
        {
            var cp = (p1 + p2) / 2 + Vector3.up * 10;
            var points = new List<Vector3>();
            const int N = 20;
            for (var i = 0; i < N; i++)
            {
                points.Add(makeBezierCurvePoint(p1, p2, cp, 1.0f / N * i));
            }
            points.Add(p2);

            _lineRenderer!.positionCount = N+1;
            _lineRenderer.SetPositions(points.ToArray());
            _lineRenderer.enabled = true;
        }

        void hideLine()
        {
            _lineRenderer!.positionCount = 0;
            _lineRenderer.enabled = false;
        }

        void init(ITransform parent, IVisualProperty property)
        {
            transform.SetParent(parent, worldPositionStays: false);
        }

        void ITargetVisualizer.Initialize(IVisualizerTarget target)
        {
            if (target is IUnit unit)
            {
                var disposable = new CompositeDisposable();
                unit.ActionRunner.OnTargetChanged.Subscribe(aimingTarget =>
                {
                    Debug.Log($"[{unit.Name}] OnTargetChanged: {aimingTarget?.Name ?? "None"}");
                    if (aimingTarget != null)
                    {
                        _transform.OnPositionChanged.Subscribe(pos => {
                            updateLine(pos, aimingTarget.Transform.Position);
                        }).AddTo(disposable);
                    }
                    else
                    {
                        disposable.Clear();
                        hideLine();
                    }
                }).AddTo(this);
            }
            init(target.Transform, target.VisualProperty);
        }

        void ITargetVisualizer.SetVisibility(bool v)
        {
            if (_lineRenderer != null) _lineRenderer.enabled = v;
        }
    }
}
