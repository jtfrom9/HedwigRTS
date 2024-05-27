#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

using UnityExtensions;
using Hedwig.RTSCore.Impl;
using System.Linq;
using PlasticGui.Help.Conditions;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/Unit", fileName = "Unit")]
    public class UnitObject : ScriptableObject, IUnitData, IUnitFactory
    {
        [SerializeField, SearchContext("p:t:prefab Unit")]
        GameObject? prefab;

        [SerializeField]
        int _MaxHealth;

        [SerializeField]
        int _Attack;

        [SerializeField]
        int _Deffence;

        [SerializeField, InspectInline]
        UnitActionObject _unitAction;

        public string Name { get => name; }
        public int MaxHealth { get => _MaxHealth; }
        public int Attack { get => _Attack; }
        public int Deffence { get => _Deffence; }
        public IUnitActionStateHolder StateHolder { get => _unitAction; }

        IUnit? IUnitFactory.Create(IUnitManager unitManager, IUnitCallback unitCallback,
            Vector3? position,
            string? name,
            IUnitController? unitController)
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
            var launcherController = unitController.Context.GetComponentInChildren<ILauncherController>();
            var unit = new UnitImpl(unitManager, this, unitController, unitCallback,
                name: name,
                launcherController: launcherController);
            unitController.Initialize(
                callback: unit,
                position,
                name,
                timeManager: unitManager.TimeManager);
            return unit;
        }
    }
}