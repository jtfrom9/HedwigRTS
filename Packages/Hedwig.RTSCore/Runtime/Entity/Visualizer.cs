#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public interface IPointIndicator : ITransformProvider
    {
        void Initialize();
        void Move(Vector3 pos);
        void SetColor(Color color);
    }

    public interface IGlobalVisualizerFactory
    {
        IPointIndicator CreatePointIndicator();
    }

    public interface IVisualizerTarget: ITransformProvider
    {
        void AddVisualizer(ITargetVisualizer targetVisualizer);

        ISelectable Selectable { get; }
        IVisualProperty VisualProperty { get; }
    }

    public interface ITargetVisualizer: IDisposable
    {
        void Initialize(IVisualizerTarget target);
        void SetVisibility(bool v);
    }

    public interface ITargetVisualizerFactory
    {
        IEnumerable<ITargetVisualizer> CreateTargetVisualizers(IVisualizerTarget target);
    }
}