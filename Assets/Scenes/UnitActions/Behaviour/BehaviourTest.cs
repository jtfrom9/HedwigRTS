#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using VContainer;
using VContainer.Unity;
using UnityExtensions;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;
using UniRx;

using Hedwig.RTSCore;
using Hedwig.RTSCore.Model;
using Hedwig.RTSCore.Model.BehaviourTree;

using Tree = Hedwig.RTSCore.Model.BehaviourTree.Tree;

public class BehaviourTest : LifetimeScope
{
    [SerializeField, InspectInline, Required] UnitManagerObject? unitManagerObject;
    [SerializeField, InspectInline, Required] UnitObject? unitObject;

#pragma warning disable CS8618
    [Inject] IUnitManager unitManager;
#pragma warning restore CS8618

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Setup(timeManager: null,
            launcherController: null,
            units: new List<UnitObject>() { unitObject! },
            unitManager: unitManagerObject,
            visualizers: null);
    }

    [Serializable]
    public class TestBehaviour1 : IPredefinedUnitBehaviour
    {
        public IUnitBehaviourExecutor Create()
        {
            return new Tree(new Sequencer(
                new IdleNode(1000),
                new PatrolActionNode(waypoints: new[] {
                    new Vector3( 5, 0, -5),
                    new Vector3( 5, 0, 5),
                    new Vector3( -5, 0, 5),
                    new Vector3( -5, 0, -5),
                    Vector3.zero
                }),
                new GeneralAction((context, unit) =>
                {
                    Debug.Log("OK");
                    context.Set<bool>("end", true);
                    return BehaviourStatus.Success;
                })
            ));
        }
    }

    async void Start()
    {
        unitManager.AutoRegisterUnitsInScene(unitObject!);

        Debug.Log("Start..");

        var unit = unitManager.Units.FirstOrDefault();
        var behaviourExecutor = unit.BehaviourExecutor;
        if (unit != null && behaviourExecutor!=null)
        {
            await UniTask.Create(async () =>
            {
                bool end = false;
                BehaviourStatus lastStatus = BehaviourStatus.InActive;
                while (!end)
                {
                    // await UniTask.NextFrame();
                    await UniTask.Delay(100);
                    var context = behaviourExecutor.Tick(unit, lastStatus);
                    lastStatus = context.Status;
                    var lanode = context.LastActionNode;
                    if (lanode != null)
                    {
                        Debug.Log($">> EndTick({context.Status}): LastAction = {lanode.Name}, Status = {lanode.LastStatus}");
                    }
                    else
                    {
                        Debug.Log($">> EndTick({context.Status})");
                    }
                    if (context.TryGet<bool>("end", out end))
                    {
                        Debug.Log(">>>>>>>>>>>> end");
                        break;
                    }
                }
            });
        }
        Debug.Log("Game End");
    }
}
