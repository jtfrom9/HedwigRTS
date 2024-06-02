using UnityEditor;
using UnityEngine;

namespace Hedwig.Editor
{
    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    public class SerializableRequiredEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var targetObject = serializedObject.targetObject;
            var targetType = targetObject.GetType();
            var fields = targetType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(SerializeReference), true))
                {
                    var fieldValue = field.GetValue(targetObject);

                    if (fieldValue != null || fieldValue is Object unityObject && unityObject == null)
                    {
                        var actualType = fieldValue.GetType();
                        // var subFields = field.FieldType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var subFields = actualType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        foreach (var subField in subFields)
                        {
                            if (subField.IsDefined(typeof(RTSCore.SerializableRequiredAttribute), true))
                            {
                                var subFieldValue = subField.GetValue(fieldValue);
                                // Debug.Log($"    >>> {subField} value = {subFieldValue}");

                                if (subFieldValue == null || subFieldValue is Object subUnityObject && subUnityObject==null)
                                {
                                    EditorGUILayout.HelpBox($"Field '{subField.Name}' in '{actualType.Name}' is required.", MessageType.Error);
                                }
                            }
                        }
                    }
                }
            }

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}