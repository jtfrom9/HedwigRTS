#nullable enable

using System.Threading;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public class GameSenario
    {
        IEnemyManager enemyManager;
        EnemyObject enemyObject;
        Vector3[] spawnPoints;
        Vector3 target;
        int spawnCondition;

        void randomSpawn()
        {
            var count = spawnCondition - enemyManager.Enemies.Count;
            if(count <= 0)
                return;
            Debug.Log($"randomSpawn: count={count}");
            for (var i = 0; i < count; i++)
            {
                var point = spawnPoints[Random.Range((int)0, (int)spawnPoints.Length - 1)];
                enemyManager.Spawn(enemyObject, point);
            }
        }

        public async UniTask Run(CancellationToken cancellationToken)
        {
            var disposable = new CompositeDisposable();

            randomSpawn();
            enemyManager.Enemies.ObserveRemove().Subscribe(_ =>
            {
                randomSpawn();
            }).AddTo(disposable);

            await UniTask.Create(async () => {
                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var enemy in enemyManager.Enemies)
                    {
                        var x = Random.Range(-5, 5);
                        var z = Random.Range(-5, 5);
                        enemy.SetDestination(target + new Vector3(x, 0, z));
                    }
                    await UniTask.Delay(3000, cancellationToken: cancellationToken);
                }
            });
            disposable.Dispose();
        }

        public GameSenario(IEnemyManager enemyManager, EnemyObject enemyObject, Vector3[] spawnPoints, Vector3 target, int spawnCondition)
        {
            this.enemyManager = enemyManager;
            this.enemyObject = enemyObject;
            this.spawnPoints = spawnPoints;
            this.target = target;
            this.spawnCondition = spawnCondition;

            if(spawnPoints.Length==0) {
                throw new InvalidConditionException("invalid spawnPoints");
            }
        }
    }
}