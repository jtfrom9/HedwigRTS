#nullable enable

using System;
using UnityEngine;

namespace Hedwig.RTSCore
{
    public interface ITransformProvider: IDisposable
    {
        ITransform Transform { get; }
    }

    public static class TransformProviderFactory
    {
        class Impl : ITransformProvider
        {
            GameObject _gameObject;
            CachedTransform _transform;

            ITransform ITransformProvider.Transform { get => _transform; }

            public void Dispose()
            {
                GameObject.Destroy(_gameObject);
            }

            public Impl(GameObject gameObject)
            {
                _gameObject = gameObject;
                _transform = new CachedTransform();
                _transform.Initialize(gameObject.transform);
            }
        }

        public static ITransformProvider AsTransformProvider(this GameObject gameObject)
        {
            return new Impl(gameObject);
        }
    }
}