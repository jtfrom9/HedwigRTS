#nullable enable

using System.Threading;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Usecase
{
    public class GameSenario
    {
        IUnitManager unitManager;
        IUnitData unitData;
        Vector3[] spawnPoints;
        Vector3 target;
        int spawnCondition;

        void randomSpawn()
        {
            var count = spawnCondition - unitManager.Units.Count;
            if(count <= 0)
                return;
            Debug.Log($"randomSpawn: count={count}");
            for (var i = 0; i < count; i++)
            {
                var point = spawnPoints[Random.Range((int)0, (int)spawnPoints.Length - 1)];
                unitManager.Spawn(unitData, point);
            }
        }

        public async UniTask Run(CancellationToken cancellationToken)
        {
            var disposable = new CompositeDisposable();

            randomSpawn();
            unitManager.Units.ObserveRemove().Subscribe(_ =>
            {
                randomSpawn();
            }).AddTo(disposable);

            await UniTask.Create(async () => {
                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var unit in unitManager.Units)
                    {
                        var x = Random.Range(-5, 5);
                        var z = Random.Range(-5, 5);
                        unit.SetDestination(target + new Vector3(x, 0, z));
                    }
                    await UniTask.Delay(3000, cancellationToken: cancellationToken);
                }
            });
            disposable.Dispose();
        }

        public GameSenario(IUnitManager unitManager, IUnitData unitData, Vector3[] spawnPoints, Vector3 target, int spawnCondition)
        {
            this.unitManager = unitManager;
            this.unitData = unitData;
            this.spawnPoints = spawnPoints;
            this.target = target;
            this.spawnCondition = spawnCondition;

            if(spawnPoints.Length==0) {
                throw new InvalidConditionException("invalid spawnPoints");
            }
        }
    }
}