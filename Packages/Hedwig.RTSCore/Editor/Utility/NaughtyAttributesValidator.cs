using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using NaughtyAttributes;

namespace Hedwig.Editor
{
    [InitializeOnLoad]
    public static class PlayModeValidator
    {
        static PlayModeValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state==PlayModeStateChange.EnteredPlayMode)
            {
                if (!ValidateRequiredScriptableObjectFields() || !ValidateRequiredMonoBehaviourFields())
                {
                    Debug.LogError("Play mode cannot start because there are unassigned required fields.");
                    EditorApplication.isPlaying = false;
                }
            }
        }

        private static bool ValidateRequiredScriptableObjectFields()
        {
            bool isValid = true;

            var scriptableObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();

            foreach (var so in scriptableObjects)
            {
                var fields = so.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (Attribute.IsDefined(field, typeof(RequiredAttribute)))
                    {
                        var value = field.GetValue(so);

                        if (value == null || value is UnityEngine.Object unityObject && unityObject == null)
                        {
                            Debug.LogError($"The field '{field.Name}' in '{so.name}' ({so.GetType().Name}) is required but not assigned.", so);
                            isValid = false;
                        }
                    }
                }
            }
            return isValid;
        }

        private static bool ValidateRequiredMonoBehaviourFields()
        {
            bool isValid = true;
            var monoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();

            foreach (var monoBehaviour in monoBehaviours)
            {
                var fields = monoBehaviour.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                foreach (var field in fields)
                {
                    if (Attribute.IsDefined(field, typeof(RequiredAttribute)))
                    {
                        var value = field.GetValue(monoBehaviour);

                        if (value == null)
                        {
                            Debug.LogError($"The field '{field.Name}' in '{monoBehaviour.GetType().Name}' is required but not assigned.", monoBehaviour);
                            isValid = false;
                        }
                    }
                }
            }
            return isValid;
        }
    }
}