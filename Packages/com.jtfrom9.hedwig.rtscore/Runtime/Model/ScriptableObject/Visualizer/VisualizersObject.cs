#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityExtensions;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Visualizer/Visualizers", fileName = "Visualizers")]
    public class VisualizersObject : ScriptableObject, IGlobalVisualizerFactory, ITargetVisualizerFactory
    {
        [SerializeField, InspectInline]
        GlobalVisualizersObject? _global;

        [SerializeField, InspectInline]
        TargetVisualizersObject? _targets;

        IFreeCursorVisualizer IGlobalVisualizerFactory.CreateFreeCursor() {
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
}