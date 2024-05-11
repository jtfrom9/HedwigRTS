#nullable enable

using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Hedwig.RTSCore
{
    public interface ITransform : IDisposable
    {
        Vector3 Position { get; }
        Quaternion Rotation { get; }

        Vector3 Right { get; }
        Vector3 Up { get; }
        Vector3 Forward { get; }

        IObservable<Vector3> OnPositionChanged { get; }
        IObservable<Quaternion> OnRotationChanged { get; }

        Transform Raw { get; }

        void Initialize(Transform transform);
    }

    public class CachedTransform : ITransform
    {
        Transform? _transform;

        Subject<Vector3>? onPositionChanged = null;
        Subject<Quaternion>? onRotationChanged = null;

        CompositeDisposable disposable = new CompositeDisposable();

        #region ITransform
        Vector3 ITransform.Position { get => (_transform != null) ? _transform.position : Vector3.zero; }
        Quaternion ITransform.Rotation { get => (_transform != null) ? _transform.rotation : Quaternion.identity; }

        Vector3 ITransform.Right { get => (_transform != null) ? _transform.right : Vector3.right; }
        Vector3 ITransform.Up { get => (_transform != null) ? _transform.up : Vector3.up; }
        Vector3 ITransform.Forward { get => (_transform != null) ? _transform.forward : Vector3.forward; }

        IObservable<Vector3> ITransform.OnPositionChanged
        {
            get
            {
                if (_transform == null)
                {
                    throw new InvalidConditionException("CachedTransform not Initialized");
                }
                if (onPositionChanged == null)
                {
                    onPositionChanged = new Subject<Vector3>();
                    _transform.ObserveEveryValueChanged(t => t.position).Subscribe(pos =>
                    {
                        onPositionChanged.OnNext(pos);
                    }).AddTo(disposable);
                }
                return onPositionChanged;
            }
        }

        IObservable<Quaternion> ITransform.OnRotationChanged
        {
            get
            {
                if (_transform == null)
                {
                    throw new InvalidConditionException("CachedTransform not Initialized");
                }
                if (onRotationChanged == null)
                {
                    onRotationChanged = new Subject<Quaternion>();
                    _transform.ObserveEveryValueChanged(t => t.rotation).Subscribe(rot =>
                    {
                        onRotationChanged.OnNext(rot);
                    }).AddTo(disposable);
                }
                return onRotationChanged;
            }
        }

        Transform ITransform.Raw
        {
            get
            {
                if (_transform == null)
                {
                    throw new InvalidConditionException("CachedTransform not Initialized");
                }
                return _transform;
            }
        }
        #endregion

        public void Dispose()
        {
            onPositionChanged?.OnCompleted();
            onRotationChanged?.OnCompleted();
            disposable.Dispose();
        }

        public void Initialize(Transform transform)
        {
            _transform = transform;
            _transform.OnDestroyAsObservable().Subscribe(_ =>
            {
                this.Dispose();
            });
        }

        public CachedTransform()
        {
        }

        internal CachedTransform(Transform transform)
        {
            Initialize(transform);
        }
    }

    public static class CachedTransformFactory
    {
        public static ITransform CachedTransform(this MonoBehaviour monoBehaviour)
        {
            return new CachedTransform(monoBehaviour.transform);
        }

        public static ITransform CachedTransform(this GameObject gameObject)
        {
            return new CachedTransform(gameObject.transform);
        }
    }

    public static class TransformExtension
    {
        public static void SetParent(this Transform transform, ITransform parent)
        {
            if (parent.Raw == null)
            {
                Debug.LogWarning($"TransformExtension.SetParent. parent.Raw is null");
            }
            transform.SetParent(parent.Raw);
        }

        public static void SetParent(this Transform transform, ITransform parent, bool worldPositionStays)
        {
            if (parent.Raw == null)
            {
                Debug.LogWarning($"TransformExtension.SetParent. parent.Raw is null");
            }
            transform.SetParent(parent.Raw, worldPositionStays);
        }
    }

    public static class ITransformExtension
    {
        public static Vector3 ShakeRandom(this ITransform transform, float shake)
        {
            if (shake == 0f)
            {
                return Vector3.zero;
            }
            else
            {
                var _r = UnityEngine.Random.Range(-shake, shake);
                var _u = UnityEngine.Random.Range(-shake, shake);
                return transform.Right * _r + transform.Up * _u;
            }
        }
    }
}
