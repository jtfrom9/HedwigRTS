#nullable enable

using System;
using UnityEngine;
using UnityEngine.Search;
using VContainer;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Visualizer/Global", fileName = "GlobalVisualizer")]
    public class GlobalVisualizersObject : ScriptableObject, IGlobalVisualizerFactory
    {
        [SerializeField, SearchContext("t:prefab visualizer")]
        GameObject? freeCursorPrefab;

        IPointIndicator IGlobalVisualizerFactory.CreatePointIndicator()
        {
            if (freeCursorPrefab == null)
            {
                throw new InvalidConditionException("no freeCursorPrefab");
            }
            var cursor = Instantiate(freeCursorPrefab).GetComponent<IPointIndicator>();
            if (cursor == null)
            {
                throw new InvalidConditionException("no IFreeCursorVisualizer");
            }
            cursor.Initialize();
            return cursor;
        }
    }

    public static class GlobalVisualizersObjectDIExtension
    {
        public static void SetupVisualizer(this IContainerBuilder builder, GlobalVisualizersObject? globalVisualizersObject)
        {
            if (globalVisualizersObject == null)
            {
                throw new ArgumentNullException("visualizersObject is null");
            }
            builder.RegisterInstance<GlobalVisualizersObject>(globalVisualizersObject).AsImplementedInterfaces();
        }
    }

}
