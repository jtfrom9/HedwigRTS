#nullable enable

using UnityEngine;
using UnityEngine.Search;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName ="Hedwig/Visualizer/TargetVisualizer", fileName ="TargetVisualizer")]
    public class TargetVisualizerObject : ScriptableObject
    {
        [SerializeField, SearchContext("t:prefab visualizer")]
        [Required]
        GameObject? prefab;

        public ITargetVisualizer Create(IVisualizerTarget target)
        {
            if (!Instantiate(prefab!).TryGetComponent<ITargetVisualizer>(out var visualizer))
            {
                throw new InvalidConditionException("No ITargetVisualizer");
            }
            visualizer.Initialize(target);
            return visualizer;
        }
    }
}