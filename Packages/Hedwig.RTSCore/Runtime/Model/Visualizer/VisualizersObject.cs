#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityExtensions;
using VContainer;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Visualizer/Visualizers", fileName = "Visualizers")]
    public class VisualizersObject : ScriptableObject, IGlobalVisualizerFactory, ITargetVisualizerFactory
    {
        [SerializeField, InspectInline]
        GlobalVisualizersObject? _global;

        [SerializeField, InspectInline]
        TargetVisualizersObject? _targets;

        IFreeCursorVisualizer IGlobalVisualizerFactory.CreateFreeCursor()
        {
            var globalFactory = (_global as IGlobalVisualizerFactory);
            if (globalFactory == null)
            {
                throw new InvalidConditionException("no global visualizer object");
            }
            return globalFactory.CreateFreeCursor();
        }

        IEnumerable<ITargetVisualizer> ITargetVisualizerFactory.CreateVisualizers(IVisualizerTarget target)
        {
            var targetsFactory = (_targets as ITargetVisualizerFactory);
            if (targetsFactory == null)
            {
                throw new InvalidConditionException("no target visualizer object");
            }
            return targetsFactory.CreateVisualizers(target);
        }
    }

    public static class VisualizersObjectDIExtension
    {
        public static void SetupVisualizer(this IContainerBuilder builder, VisualizersObject? visualizersObject)
        {
            if (visualizersObject == null)
            {
                throw new ArgumentNullException("visualizersObject is null");
            }
            builder.RegisterInstance<VisualizersObject>(visualizersObject).AsImplementedInterfaces();
        }
    }
}