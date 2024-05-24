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
        IReadOnlyReactiveCollection<IUnit> Units { get; }
        IUnit Spawn(IUnitFactory unitFactory, Vector3 position, string? name = null);

        void Initialize(IUnitData defaultUnitObject);
    }

    public static class UnitManagerExtension
    {
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
    }
}
