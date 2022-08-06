#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public interface IEnvironmentController: ITransformProvider
    {
        void Initialize(IEnvironmentEvent environmentEvent);
        string name { get; }
    }

    public interface IEnvironmentEvent
    {
        void OnHit(IHitObject hitObject);
    }

    public interface IEnvironment
    {
        IEnvironmentController controller { get; }
    }
}