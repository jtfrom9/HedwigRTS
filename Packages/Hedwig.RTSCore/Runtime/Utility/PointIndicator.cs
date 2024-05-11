#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using UniRx.Triggers;

namespace Hedwig.RTSCore
{
    public class PointIndicator : MonoBehaviour
    {
        [SerializeField]
        TextMeshPro? textMesh = null;

        // [SerializeField]
        Vector3 textOffset = new Vector3(0, 1, 0);

        [SerializeField]
        float lineWidth = .05f;

        [SerializeField]
        Color lineColor = Color.white;

        [SerializeField]
        bool showLine = true;

        [SerializeField]
        bool showPoint = false;

        Camera? _camera = null;
        Transform? _edge = null;

        void Start()
        {
            if (textMesh == null)
            {
                throw new InvalidConditionException("no text mesh");
            }
            setupTargetPoint();
            setupTextMesh(textMesh);
            setupUnderline(textMesh);
            setupPointToEdgeLine(textMesh);
        }

        Transform makePoint(string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(.1f, .1f, .1f);
            go.transform.SetParent(parent);
            go.name = name;

            var mr = go.GetComponent<MeshRenderer>();
            this.ObserveEveryValueChanged(self => self.showPoint).Subscribe(v =>
            {
                mr.enabled = v;
            }).AddTo(this);

            return go.transform;
        }

        LineRenderer makeLine(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lineColor;
            lr.endColor = lineColor;

            this.ObserveEveryValueChanged(self => self.lineWidth).Subscribe(width =>
            {
                lr.startWidth = width;
                lr.endWidth = width;
            }).AddTo(this);

            this.ObserveEveryValueChanged(self => self.lineColor).Subscribe(color =>
            {
                lr.startColor = lineColor;
                lr.endColor = lineColor;
            }).AddTo(this);

            this.ObserveEveryValueChanged(self => self.showLine).Subscribe(v =>
            {
                lr.enabled = v;
            }).AddTo(this);

            return lr;
        }

        void setupTargetPoint()
        {
            makePoint("Target", this.transform);
        }

        void setupTextMesh(TextMeshPro textMesh)
        {
            this.ObserveEveryValueChanged(self => self.textOffset).Subscribe(offset =>
            {
                textMesh.transform.localPosition = offset;
            }).AddTo(this);

            this.UpdateAsObservable().Subscribe(_ =>
            {
                var camera = _camera ?? Camera.main;
                // transform.LookAt(transform.position - camera.transform.position);
                // transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
                // transform.LookAt(transform.position - camera.transform.position, camera.transform.up);
                // var dir = transform.position - camera.transform.position;
                // var look = camera.transform.position + 2 * dir;
                // transform.LookAt(look, camera.transform.up);
                transform.LookAt(transform.position - (-camera.transform.forward), camera.transform.up);
            }).AddTo(this);
        }

        void setupUnderline(TextMeshPro textMesh)
        {
            var lr = makeLine("Underline", textMesh.transform);
            lr.useWorldSpace = false;
            lr.positionCount = 2;

            textMesh.ObserveEveryValueChanged((t) => t.bounds).Subscribe(bounds =>
            {
                var bottom = bounds.center.y - bounds.extents.y;
                var left = bounds.center.x - bounds.extents.x;
                var right = bounds.center.x + bounds.extents.x;
                lr.SetPositions(new Vector3[] {
                    new Vector3(left, bottom,0),
                    new Vector3(right, bottom, 0)
                });
            }).AddTo(this);
        }

        Vector3 edgePosition(Bounds bounds, Vector2 pos) {
            var top = bounds.center.y + bounds.extents.y;
            var bottom = bounds.center.y - bounds.extents.y;
            var left = bounds.center.x - bounds.extents.x;
            var right = bounds.center.x + bounds.extents.x;

            // var y = (pos.x >= 0) ? top : bottom;
            float y = 0;
            if(pos.x >=0) {
                y = top;
                textOffset = new Vector3(0, -1, 0);
            }else {
                y = bottom;
                textOffset = new Vector3(0, 1, 0);
            }

            if (pos.y >= 0)
            {
                return new Vector3(left, y, 0);
            }else
            {
                return new Vector3(right, y, 0);
            }
        }

        void updateEdge(Bounds bounds, Vector2 cross) {
            if (_edge != null)
            {
                _edge.transform.localPosition = edgePosition(bounds, cross);
            }
        }

        void setupPointToEdgeLine(TextMeshPro textMesh)
        {
            _edge = makePoint("Edge", textMesh.transform);
            var lr = makeLine("EdgeLine", textMesh.transform);
            lr.useWorldSpace = true;
            lr.positionCount = 2;

            // Edge position update reactived from textMesh bounds update
            textMesh.ObserveEveryValueChanged((t) => t.bounds).Subscribe(bounds =>
            {
                // _edge.transform.localPosition = edgePosition(bounds, new Vector2(0, 0));
                updateEdge(bounds, new Vector2(0, 0));
            }).AddTo(this);

            // Redraw edge to point line reactived from edge position update
            _edge.transform.ObserveEveryValueChanged(t => t.position).Subscribe(pos =>
            {
                lr.SetPositions(new Vector3[] {
                    pos,
                    transform.position
                });
            }).AddTo(this);

        }

        public void SetText(string text)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
            }
        }

        public void SetCamera(Camera camera)
        {
            this._camera = camera;

            this.UpdateAsObservable().Subscribe(_ =>
            {
                var viewdir = camera.transform.position + camera.transform.forward;
                // Debug.DrawLine(camera.transform.position, viewdir, Color.red, 1);
                // Debug.DrawRay(camera.transform.position, camera.transform.forward * 10, Color.red);
                // Debug.DrawRay(camera.transform.position, this.transform.position - camera.transform.position, Color.blue);
                // var angle = Vector3.Angle(camera.transform.forward, this.transform.position - camera.transform.position);
                // var dot = Vector3.Dot(camera.transform.forward, this.transform.position - camera.transform.position);
                var cross = Vector3.Cross(camera.transform.forward, this.transform.position - camera.transform.position);
                // Debug.Log($"angle = {angle}, dot = {dot}, corss = {cross}");
                // SetColor(cross.y < 0 ? Color.red : Color.blue);

                if(_edge!=null && textMesh!=null) {
                    // Debug.Log($"{cross.y}");
                    // _edge.localPosition = edgePosition(textMesh.bounds, cross);
                    updateEdge(textMesh.bounds, cross);
                }

            }).AddTo(this);
        }

        public void SetOffset(in Vector3 offset)
        {
            this.textOffset = offset;
        }

        public void SetWidth(float width)
        {
            this.lineWidth = width;
        }

        public void SetColor(Color color)
        {
            this.lineColor = color;
        }
    }
}