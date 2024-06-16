#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using UniRx;
using Cysharp.Threading.Tasks;
using PlasticGui.WorkspaceWindow;
using System.Linq;

namespace Hedwig.RTSCore
{
    public interface IUnitManager : IDisposable
    {
        IReadOnlyReactiveCollection<IUnit> Units { get; }
        IUnit Spawn(IUnitData unitData, Vector3 position, string? name = null, string? tag = null);

        void Register(IUnitController unitController, IUnitData data);
    }

    public static class UnitManagerExtension
    {
        public static void AutoRegisterUnitsInScene(this IUnitManager manager, IUnitData data)
        {
            var unitControllerReposiotry = ControllerBase.Find<IUnitControllerRepository>();
            if (unitControllerReposiotry != null)
            {
                foreach (var controller in unitControllerReposiotry.GetUnitControllers())
                {
                    manager.Register(controller, data);
                }
            }
        }

        public static UniTask RandomWalk(this IUnitManager manager, float min, float max, int msec, CancellationToken token)
        {
            return UniTask.Create(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var unit in manager.Units)
                    {
                        var x = UnityEngine.Random.Range(min, max);
                        var z = UnityEngine.Random.Range(min, max);
                        var pos = new Vector3(x, 0, z);
                        unit.SetDestination(pos);
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
            foreach(var unit in manager.Units) {
                unit.Stop();
            }
        }

        public static IUnit? ChoiceOne(this IUnitManager manager, IUnit self)
        {
            foreach(var unit in manager.Units) {
                if (unit != self)
                {
                    return unit;
                }
            }
            return null;
        }

        public static async UniTask RunBehaviourLoop(this IUnitManager manager, int msec, CancellationToken ct = default)
        {
            var tasks = new List<UniTask>();
            foreach (var unit in manager.Units.Where(unit => unit.BehaviourExecutor != null))
            {
                tasks.Add(UniTask.Create(async () =>
                {
                    BehaviourStatus lastStatus = BehaviourStatus.InActive;
                    while (!ct.IsCancellationRequested)
                    {
                        var context = unit.BehaviourExecutor!.Tick(unit, lastStatus);
                        var lastAction = context.LastActionNode;
                        lastStatus = context.Status;
                        if (lastAction != null)
                        {
                            Debug.Log($"{unit.Name}[Tick] Action: {lastAction.Name}, Status:{lastStatus}");
                        } else {
                            Debug.Log($"{unit.Name}[Tick] Action: N/A");
                        }

                        await UniTask.Delay(msec, cancellationToken: ct);
                    }
                }));
            }
            await UniTask.WhenAll(tasks);
        }
    }
}
