#nullable enable

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Hedwig.RTSCore
{
    public static class UtilityExtension
    {
        public static DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>
            DORotateAround(this Transform transform, Vector3 point, Vector3 axis, float endValue, float duration)
        {
            float anglePrev = 0;
            return DOTween.To(
                () => 0,
                (angle) =>
                {
                    transform.RotateAround(point, axis, angle - anglePrev);
                    anglePrev = angle;
                },
                endValue,
                duration);
        }

        public static Vector3 X(this Vector3 vec, float x)
        {
            return new Vector3(x, vec.y, vec.z);
        }

        public static Vector3 Y(this Vector3 vec, float y)
        {
            return new Vector3(vec.x, y, vec.z);
        }

        public static Vector3 Z(this Vector3 vec, float z)
        {
            return new Vector3(vec.x, vec.y, z);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
                where T : class
        {
            if (source == null)
            {
                return Enumerable.Empty<T>();
            }

            return source.Where(x => x != null)!;
        }
    }
}
