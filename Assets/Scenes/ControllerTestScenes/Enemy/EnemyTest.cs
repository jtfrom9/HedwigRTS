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

public class EnemyTest : LifetimeScope
{
    [SerializeField]
    EnemyObject? defaultEnemyObject;

    [SerializeField]
    EnemyManagerObject? enemyManagerObject;

    [SerializeField]
    VisualizersObject? visualizersObject;

    [SerializeField]
    Text? text;

    [Inject]
    IEnemyManager? enemyManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance<EnemyManagerObject>(enemyManagerObject!);
        builder.RegisterInstance<VisualizersObject>(visualizersObject!)
            .AsImplementedInterfaces();
        builder.Register<IEnemyManager, EnemyManagerImpl>(Lifetime.Singleton);
    }

    void Start()
    {
        if(enemyManager==null || defaultEnemyObject==null) return;
        if(text==null) return;

        enemyManager.Initialize(defaultEnemyObject);

        RnadomMoveEnemy(enemyManager).Forget();
        RandomAttach(enemyManager).Forget();

        Camera.main.transform.position = new Vector3(0, 1, -20);
        Camera.main.transform.DORotateAround(Vector3.zero, Vector3.up, 360, 10)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

        this.UpdateAsObservable().Subscribe(_ =>
        {
            text.text = $"# of enemy: {enemyManager.Enemies.Count}";
            foreach (var e in enemyManager.Enemies)
            {
                text.text += $"\n {e}: {e.Health}";
            }

            if (enemyManager.Enemies.Count == 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit ();
#endif
            }
        }).AddTo(this);
    }

    async UniTaskVoid RnadomMoveEnemy(IEnemyManager enemyManager)
    {
        while (enemyManager.Enemies.Count > 0)
        {
            foreach (var enemy in enemyManager.Enemies)
            {
                var x = Random.Range(-30f, 30f);
                var z = Random.Range(-30f, 30f);
                enemy.SetDestination(new Vector3(x, 0, z));
            }
            await UniTask.Delay(3000);
        }
    }

    async UniTaskVoid RandomAttach(IEnemyManager enemyManager)
    {
        while (enemyManager.Enemies.Count > 0)
        {
            await UniTask.Delay(1000);
            foreach (var enemy in enemyManager.Enemies)
            {
                enemy.Damaged(Random.Range(1, 10));
            }
        }
    }
}
