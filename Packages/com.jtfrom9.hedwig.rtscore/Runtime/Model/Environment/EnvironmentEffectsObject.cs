#nullable enable

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Environment/Effects", fileName = "EnvironmentEffects")]
    public class EnvironmentEffectsObject : ScriptableObject
    {
        [SerializeField]
        List<HitEffect> hitEffects = new List<HitEffect>();

        IEnumerable<IEffect?> createEffects(IEnvironment environment, Vector3 position, Vector3 direction)
        {
            foreach (var effect in hitEffects)
            {
                yield return effect.Create(environment.controller, position, direction);
            }
        }

        public IEffect[] CreateEffects(IEnvironment environment, Vector3 position, Vector3 direction)
            => createEffects(environment, position, direction)
                .WhereNotNull()
                .ToArray();
    }
}