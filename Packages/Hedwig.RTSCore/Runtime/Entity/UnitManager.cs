#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using UniRx;
using Cysharp.Threading.Tasks;
using PlasticGui.WorkspaceWindow;

namespace Hedwig.RTSCore
{
    public interface IUnitManager : IDisposable
    {
        IReadOnlyReactiveCollection<IUnit> Enemies { get; }
        IUnit Spawn(IUnitFactory enemyFactory, Vector3 position, string? name = null);

        void Initialize(IUnitData defaultUnitObject);
    }

    public static class EnemyManagerExtension
    {
        public static UniTask RandomWalk(this IUnitManager manager, float min, float max, int msec, CancellationToken token)
        {
            return UniTask.Create(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var enemy in manager.Enemies)
                    {
                        var x = UnityEngine.Random.Range(min, max);
                        var z = UnityEngine.Random.Range(min, max);
                        var pos = new Vector3(x, 0, z);
                        enemy.SetDestination(pos);
                    }
                    try
                    {
                        await UniTask.Delay(msec, cancellationToken: token);
                    }catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                // manager.StopAll();
            });
        }

        public static void StopAll(this IUnitManager manager)
        {
            foreach(var enemy in manager.Enemies) {
                enemy.Stop();
            }
        }

        public static IUnit? ChoiceOne(this IUnitManager manager, IUnit self)
        {
            foreach(var enemy in manager.Enemies) {
                if (enemy != self)
                {
                    return enemy;
                }
            }
            return null;
        }
    }
}
