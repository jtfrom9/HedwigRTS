#nullable enable

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityExtensions;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Environment/Envronment", fileName = "Environment")]
    public class EnvironmentObject : ScriptableObject
    {
        [SerializeField, SearchContext("t:prefab environment")]
        GameObject? prefab;

        [SerializeField, InspectInline]
        public EnvironmentEffectsObject? environmentEffects;

        public IEnvironment? Create()
        {
            if (prefab == null) return null;
            var environmentController = Instantiate(prefab).GetComponent<IEnvironmentController>();
            if (environmentController == null) return null;
            var environment = new EnvironmentImpl(this, environmentController);
            environmentController.Initialize(environment);
            return environment;
        }
    }
}