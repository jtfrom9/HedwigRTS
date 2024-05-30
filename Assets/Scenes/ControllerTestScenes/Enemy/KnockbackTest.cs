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
using UnityExtensions;

using Hedwig.RTSCore.Model;
using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Test
{
    public class KnockbackTest : LifetimeScope
    {
        [SerializeField]
        UnitObject? defaultUnitObject;

        [SerializeField]
        UnitManagerObject? unitManagerObject;

        [SerializeField]
        GlobalVisualizersObject? globalVisualizersObject;

        [SerializeField, InspectInline]
        List<ProjectileObject> projectileObjects = new List<ProjectileObject>();

        [SerializeField]
        TextMeshProUGUI? textMesh;

        [Inject] readonly IUnitManager? enemyManager;
        [Inject] readonly ILauncher? launcher;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Setup(timeManager: null,
                launcherController: ControllerBase.Find<ILauncherController>(),
                unit: defaultUnitObject,
                unitManager: unitManagerObject,
                visualizers: globalVisualizersObject);
        }

        void Start()
        {
            Assert.IsNotNull(enemyManager);
            Assert.IsNotNull(defaultUnitObject);
            Assert.IsNotNull(textMesh);
            _start(enemyManager!, launcher!, defaultUnitObject!);
        }

        void _start(IUnitManager enemyManager, ILauncher launcher, UnitObject defaultUnitObject)
        {
            enemyManager.AutoRegisterUnitsInScene(defaultUnitObject);

            var configSelection = new Selection<ProjectileObject>(projectileObjects);
            configSelection.OnCurrentChanged.Subscribe(projectileObject =>
            {
                launcher.SetProjectile(projectileObject);
                updateText(projectileObject);
            }).AddTo(this);
            configSelection.Select(0);

            var enemy = enemyManager.Units[0];
            enemy.SetDestination(Vector3.zero);
            launcher.SetTarget(enemy.Controller);

            setupKey(configSelection, launcher, enemy);
        }

        void setupKey(Selection<ProjectileObject> configSelection, ILauncher launcher, IUnit enemy)
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