#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

using UniRx;
using UniRx.Triggers;
using VContainer;
using VContainer.Unity;

using Hedwig.RTSCore.Model;
using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Test
{
    public class KnockbackTest : LifetimeScope
    {
        [SerializeField]
        EnemyObject? defaultEnemyObject;

        [SerializeField]
        EnemyManagerObject? enemyManagerObject;

        [SerializeField]
        GlobalVisualizersObject? globalVisualizersObject;

        [SerializeField]
        List<ProjectileObject> projectileObjects = new List<ProjectileObject>();

        [SerializeField]
        TextMeshProUGUI? textMesh;

        [Inject] IEnemyManager? enemyManager;
        [Inject] ILauncher? launcher;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.SetupEnemyManager(enemyManagerObject);
            builder.SetupVisualizer(globalVisualizersObject);
            builder.SetupLauncher();
        }

        void Start()
        {
            Assert.IsNotNull(enemyManager);
            Assert.IsNotNull(defaultEnemyObject);
            Assert.IsNotNull(launcher);
            Assert.IsNotNull(textMesh);
            _start(enemyManager!, launcher!, defaultEnemyObject!);
        }

        void _start(IEnemyManager enemyManager, ILauncher launcher, EnemyObject defaultEnemyObject)
        {
            enemyManager.Initialize(defaultEnemyObject);
            launcher.Initialize();

            var configSelection = new Selection<ProjectileObject>(projectileObjects);
            configSelection.OnCurrentChanged.Subscribe(projectileObject =>
            {
                launcher.SetProjectile(projectileObject);
                updateText(projectileObject);
            }).AddTo(this);
            configSelection.Select(0);

            var enemy = enemyManager.Enemies[0];
            enemy.SetDestination(Vector3.zero);
            launcher.SetTarget(enemy.controller);

            setupKey(configSelection, launcher, enemy);
        }

        void setupKey(Selection<ProjectileObject> configSelection, ILauncher launcher, IEnemy enemy)
        {
            this.UpdateAsObservable().Subscribe(_ =>
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    configSelection.Prev();
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    configSelection.Next();
                }
                if(Input.GetKey(KeyCode.Space))
                {
                    if(launcher.CanFire.Value)
                        launcher.Fire();
                }
                if (Input.GetKey(KeyCode.Escape))
                {
                    enemy.ResetPos();
                }
            }).AddTo(this);
        }

        void updateText(ProjectileObject config)
        {
            if (textMesh != null)
            {
                if (config.weaponData != null)
                {
                    textMesh.text = $"{config.name}: attack: {config.weaponData.Attack}, power: {config.weaponData.Power}";
                }
            }
        }
    }
}