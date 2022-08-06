#nullable enable

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Visualizer/TargetVisualizers", fileName = "TargetVisualizers")]
    public class TargetVisualizersObject : ScriptableObject, ITargetVisualizerFactory
    {
        [SerializeField, InspectInline]
        List<TargetVisualizerObject> visualizerObjects = new List<TargetVisualizerObject>();

        IEnumerable<ITargetVisualizer?> createVisualizers(IVisualizerTarget target)
        {
            foreach (var vobj in visualizerObjects)
            {
                yield return vobj.Create(target);
            }
        }

        IEnumerable<ITargetVisualizer> ITargetVisualizerFactory.CreateVisualizers(IVisualizerTarget target)
            => createVisualizers(target).WhereNotNull();
    }
}
