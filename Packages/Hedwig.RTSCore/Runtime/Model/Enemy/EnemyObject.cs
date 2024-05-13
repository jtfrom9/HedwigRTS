#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Enemy/Enemy", fileName = "Enemy")]
    public class EnemyObject : ScriptableObject, IEnemyData, IEnemyFactory
    {
        [SerializeField, SearchContext("t:prefab Enemy")]
        GameObject? prefab;

        [SerializeField]
        int _MaxHealth;

        [SerializeField]
        int _Attack;

        [SerializeField]
        int _Deffence;

        public string Name { get => name; }
        public int MaxHealth { get => _MaxHealth; }
        public int Attack { get => _Attack; }
        public int Deffence { get => _Deffence; }

        IEnemy? IEnemyFactory.Create(IEnemyEvent enemyEvent, Vector3? position)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab);
            var enemyController = go.GetComponent<IEnemyController>();
            if (enemyController == null)
            {
                Destroy(go);
                return null;
            }
            var enemy = new EnemyImpl(this, enemyController, enemyEvent);
            enemyController.Initialize(this.name, enemy, position);
            return enemy;
        }
    }
}