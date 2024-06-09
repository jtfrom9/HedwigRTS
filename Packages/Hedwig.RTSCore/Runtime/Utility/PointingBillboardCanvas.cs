#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Hedwig.RTSCore
{
    [RequireComponent(typeof(Canvas))]
    public class PointingBillboardCanvas : MonoBehaviour
    {
        Canvas? _canvas;
        Transform? _transform;
        Transform? _mainCameraTransform;
        Vector3 _point = default;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _transform = GetComponent<Transform>();
            _mainCameraTransform = Camera.main.transform;
        }

        void Update()
        {
            if (_mainCameraTransform != null)
            {
                _transform?.LookAt(_mainCameraTransform.position, _mainCameraTransform.up);
            }
        }
    }
}