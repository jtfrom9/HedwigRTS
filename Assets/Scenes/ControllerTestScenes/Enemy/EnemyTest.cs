#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using VContainer;
using VContainer.Unity;

using Hedwig.RTSCore;
using Hedwig.RTSCore.Model;

public class EnemyTest : LifetimeScope
{
    [SerializeField]
    UnitObject? defaultUnitObject;

    [SerializeField]
    UnitManagerObject? unitManagerObject;

    [SerializeField]
    GlobalVisualizersObject? globalVisualizersObject;

    [SerializeField]
    Text? text;

    [Inject]
    IUnitManager? enemyManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Setup(timeManager: null,
            launcherController: null,
            unit: defaultUnitObject,
            unitManager: unitManagerObject,
            visualizers: globalVisualizersObject);
    }

    void Start()
    {
        if(enemyManager==null || defaultUnitObject==null) return;
        if(text==null) return;

        enemyManager.AutoRegisterUnitsInScene(defaultUnitObject);

        RnadomMoveEnemy(enemyManager).Forget();
        RandomAttach(enemyManager).Forget();

        Camera.main.transform.position = new Vector3(0, 1, -20);
        Camera.main.transform.DORotateAround(Vector3.zero, Vector3.up, 360, 10)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

        this.UpdateAsObservable().Subscribe(_ =>
        {
            text.text = $"# of enemy: {enemyManager.Units.Count}";
            foreach (var e in enemyManager.Units)
            {
                text.text += $"\n {e}: {e.Health}";
            }

            if (enemyManager.Units.Count == 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit ();
#endif
            }
        }).AddTo(this);
    }

    async UniTaskVoid RnadomMoveEnemy(IUnitManager enemyManager)
    {
        while (enemyManager.Units.Count > 0)
        {
            foreach (var enemy in enemyManager.Units)
            {
                var x = Random.Range(-30f, 30f);
                var z = Random.Range(-30f, 30f);
                enemy.SetDestination(new Vector3(x, 0, z));
            }
            await UniTask.Delay(3000);
        }
    }

    async UniTaskVoid RandomAttach(IUnitManager enemyManager)
    {
        while (enemyManager.Units.Count > 0)
        {
            await UniTask.Delay(1000);
            foreach (var enemy in enemyManager.Units)
            {
                enemy.Damaged(Random.Range(1, 10));
            }
        }
    }
}
