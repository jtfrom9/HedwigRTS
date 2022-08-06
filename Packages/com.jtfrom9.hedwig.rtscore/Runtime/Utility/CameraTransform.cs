#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore
{
    public class CameraTransform
    {
        public static ITransform? Find()
        {
            var camera = Object.FindObjectOfType<Camera>();
            if (camera == null) { return null; }
            return camera.gameObject.CachedTransform();
        }
    }
}
