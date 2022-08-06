#nullable enable

using UnityEngine;
using UnityEngine.Search;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Visualizer/Global", fileName = "GlobalVisualizer")]
    public class GlobalVisualizersObject : ScriptableObject, IGlobalVisualizerFactory
    {
        [SerializeField, SearchContext("t:prefab visualizer")]
        GameObject? freeCursorPrefab;

        IFreeCursorVisualizer IGlobalVisualizerFactory.CreateFreeCursor()
        {
            if (freeCursorPrefab == null)
            {
                throw new InvalidConditionException("no freeCursorPrefab");
            }
            var cursor = Instantiate(freeCursorPrefab).GetComponent<IFreeCursorVisualizer>();
            if (cursor == null)
            {
                throw new InvalidConditionException("no IFreeCursorVisualizer");
            }
            cursor.Initialize();
            return cursor;
        }
    }
}
