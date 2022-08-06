#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public static class CameraWorkExtension
    {
        static void killTween(this Camera camera)
        {
            if (DOTween.IsTweening(camera.transform))
            {
                camera.transform.DOKill();
            }
        }

        public static void ResetPosition(this Camera camera, Vector3 pos, Quaternion rot)
        {
            camera.killTween();
            camera.transform.SetParent(null);
            camera.transform.SetPositionAndRotation(pos, rot);
        }

        public static void MoveWithLookAt(this Camera camera, Vector3 destination, Vector3 lookat, float duration)
        {
            camera.killTween();
            if (camera.transform.position != destination)
            {
                camera.transform.SetParent(null);
                camera.transform.DOMove(destination, duration)
                    .OnUpdate(() => camera.transform.LookAt(lookat));
            }
            else
            {
                camera.transform.DOLookAt(lookat, duration);
            }
        }

        public static void Tracking(this Camera camera, Transform target, Vector3 localPosition, Vector3 localRotation, float duration)
        {
            camera.killTween();
            camera.transform.SetParent(target, true);
            camera.transform.DOLocalMove(localPosition, duration);
            camera.transform.DOLocalRotate(localRotation, duration);
        }

        public static void Tracking(this Camera camera, ITransform target, Vector3 localPosition, Vector3 localRotation, float duration)
        {
            camera.killTween();
            camera.transform.SetParent(target, true);
            camera.transform.DOLocalMove(localPosition, duration);
            camera.transform.DOLocalRotate(localRotation, duration);
        }
    }
}
