#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

using UnityExtensions;
using Hedwig.RTSCore.Impl;
using System.Linq;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Enemy/Enemy", fileName = "Enemy")]
    public class UnitObject : ScriptableObject, IUnitData, IUnitFactory
    {
        [SerializeField, SearchContext("t:prefab Enemy")]
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

        IUnit? IUnitFactory.Create(IUnitManager manager, IUnitCallback enemyEvent, Vector3? position, string? name)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab);
            var enemyController = go.GetComponent<IUnitController>();
            if (enemyController == null)
            {
                Destroy(go);
                return null;
            }
            var enemy = new UnitImpl(manager, this, enemyController, enemyEvent, name);
            enemyController.Initialize(enemy, position, name);
            return enemy;
        }
    }
}