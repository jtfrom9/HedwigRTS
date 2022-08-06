#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore.Controller
{
    public class EnvironmentController : ControllerBase, IEnvironmentController, IHitHandler
    {
        string _name = "";
        IEnvironmentEvent? environmentEvent = null;
        ITransform _transform = new CachedTransform();

        bool _disposed = false;

        void Awake()
        {
            _transform.Initialize(transform);
        }

        void OnDestroy()
        {
            _disposed = true;
        }

        ITransform ITransformProvider.transform { get => _transform; }

        void IDisposable.Dispose()
        {
            Destroy(gameObject);
        }

        void IHitHandler.OnHit(IHitObject hitObject)
        {
            environmentEvent?.OnHit(hitObject);
        }

        static int count = 0;
        [RuntimeInitializeOnLoadMethod]
        void _InitializeOnEnterPlayMode()
        {
            count = 0;
        }

        string IEnvironmentController.name { get => _name; }
        void IEnvironmentController.Initialize(IEnvironmentEvent environmentEvent)
        {
            if(gameObject.name=="") {
                gameObject.name = $"Environment({count})";
                count++;
            }
            _name = gameObject.name;
            this.environmentEvent = environmentEvent;
        }
    }
}