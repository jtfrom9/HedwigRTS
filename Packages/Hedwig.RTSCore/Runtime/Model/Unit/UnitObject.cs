#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

using UnityExtensions;
using VContainer;
using VContainer.Unity;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/Unit", fileName = "Unit")]
    public class UnitObject : ScriptableObject, IUnitData
    {
        [SerializeField, SearchContext("p:t:prefab Unit")]
        GameObject? prefab;

        [SerializeField]
        int _MaxHealth;

        [SerializeField]
        int _Deffence;

        [SerializeField]
        float _Speed = 3.0f;

        [SerializeField, InspectInline]
        UnitActionObject _unitAction;

        public string Name { get => name; }
        public int MaxHealth { get => _MaxHealth; }
        public int Deffence { get => _Deffence; }
        public float Speed { get => _Speed; }
        public IUnitActionStateHolder StateHolder { get => _unitAction; }

        IUnit? CreateUnit(IUnitManager unitManager, IUnitCallback unitCallback,
            IUnitController unitController,
            ILauncherFactory launcherFactory,
            Vector3? position,
            string? name)
        {
            if (unitController == null)
            {
                if (prefab == null) return null;
                var go = Instantiate(prefab);
                unitController = go.GetComponent<IUnitController>();
                if (unitController == null)
                {
                    Destroy(go);
                    return null;
                }
            }
            var unit = new UnitImpl(unitManager, this, unitController, unitCallback,
                name: name,
                launcher: unitController.LauncherController != null ? launcherFactory.Invoke(unitController.LauncherController) : null);
            unitController.Initialize(
                this,
                callback: unit,
                position,
                name);
            return unit;
        }

        public IUnit? CreateUnitWithResolver(
            IObjectResolver resolver,
            Vector3? position,
            string? name,
            IUnitController? unitController = null)
        {
            var manager = resolver.Resolve<IUnitManager>();
            var unitCallback = resolver.Resolve<IUnitCallback>();
            var launcherFactory = resolver.Resolve<ILauncherFactory>();
            if (unitController == null)
            {
                if (prefab == null) return null;
                var go = resolver.Instantiate(prefab);
                unitController = go.GetComponent<IUnitController>();
                if (unitController == null)
                {
                    Destroy(go);
                    return null;
                }
            } else {
                resolver.Inject(unitController);
            }
            return CreateUnit(manager, unitCallback, unitController, launcherFactory, position, name);
        }
    }

    public static class UnitObjectDIExtension
    {
        public static void RegisterUnits(this IContainerBuilder builder, IEnumerable<UnitObject> unitObjects)
        {
            var unitObjectDict = unitObjects.ToDictionary(uobj => uobj as IUnitData, uobj => uobj);

            builder.Register<IUnitFacory>(resolver => (unitData, position, name, unitController) =>
            {
                if (unitObjectDict.TryGetValue(unitData, out var unitObject))
                {
                    return unitObject.CreateUnitWithResolver(resolver,
                        position,
                        name,
                        unitController);
                }
                else
                {
                    return null;
                }
            }, Lifetime.Singleton);
        }

        public static void RegisterUnit(this IContainerBuilder builder, UnitObject unitObject)
        {
            builder.RegisterUnits(new List<UnitObject>() { unitObject });
        }
   }
}