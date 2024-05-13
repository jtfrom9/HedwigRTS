#nullable enable

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityExtensions;
using VContainer;

using Hedwig.RTSCore.Impl;
using System;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Environment/Envronment", fileName = "Environment")]
    public class EnvironmentObject : ScriptableObject, IEnvironmentFactory, IEnvironmentEffectFactory
    {
        [SerializeField, SearchContext("t:prefab environment")]
        GameObject? prefab;

        [SerializeField, InspectInline]
        List<HitEffect> hitEffects = new List<HitEffect>();

        IEnumerable<IEffect?> createEffects(IEnvironment environment, Vector3 position, Vector3 direction)
        {
            foreach (var effect in hitEffects)
            {
                yield return effect.Create(environment.Controller, position, direction);
            }
        }

        IEffect[] IEnvironmentEffectFactory.CreateEffects(IEnvironment environment, Vector3 position, Vector3 direction)
            => createEffects(environment, position, direction)
                .WhereNotNull()
                .ToArray();

        IEnvironment? IEnvironmentFactory.Create()
        {
            if (prefab == null) return null;
            var environmentController = Instantiate(prefab).GetComponent<IEnvironmentController>();
            if (environmentController == null) return null;
            var environment = new EnvironmentImpl(this, environmentController);
            environmentController.Initialize(environment);
            return environment;
        }
    }

    public static class EnvironmentObjectDIExtension
    {
        public static void SetupEnvironment(this IContainerBuilder builder, EnvironmentObject? environmentObject)
        {
            if (environmentObject == null)
            {
                throw new ArgumentNullException("environmentObject is null");
            }
            builder.RegisterInstance<EnvironmentObject>(environmentObject).AsImplementedInterfaces();
            builder.Register<EnvironmentImpl>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterInstance<IEnvironmentController>(ControllerBase.Find<IEnvironmentController>());
        }
    }
}