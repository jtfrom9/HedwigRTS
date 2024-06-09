#nullable enable

using System;
using UnityEngine;
using UniRx;
using System.Collections.Generic;

namespace Hedwig.RTSCore.Controller
{
    public class UnitActionStateDebugger : MonoBehaviour, ITargetVisualizer
    {
        public void Initialize(IVisualizerTarget target){}
        public void SetVisibility(bool v){}
        public void Dispose()
        {}
    }
}
