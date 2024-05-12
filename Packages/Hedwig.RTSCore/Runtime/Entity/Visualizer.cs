#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public interface IFreeCursorVisualizer : ITransformProvider
    {
        void Initialize();
        void Move(Vector3 pos);
    }

    public interface IGlobalVisualizerFactory
    {
        IFreeCursorVisualizer CreateFreeCursor();
    }

    public interface IVisualizerTarget
    {
        void AddVisualizer(ITargetVisualizer targetVisualizer);

        ITransform? transform { get; }
        ISelectable? selectable { get; }
        IVisualProperty? visualProperty { get; }
        ICharactor? charactor { get; }
    }

    public interface ITargetVisualizer: IDisposable
    {
        void Initialize(IVisualizerTarget target);
    }

    public interface ITargetVisualizerFactory
    {
        IEnumerable<ITargetVisualizer> CreateTargetVisualizers(IVisualizerTarget target);
    }
}