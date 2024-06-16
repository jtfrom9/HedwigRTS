#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Cinemachine;

using VContainer;
using VContainer.Unity;
using UnityExtensions;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;

using Hedwig.RTSCore;
using Hedwig.RTSCore.Model;

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

    async void Start()
    {
        unitManager.AutoRegisterUnitsInScene(unitObject!);

        Debug.Log("Start..");

        var unit = unitManager.Units.FirstOrDefault();
        if (unit != null)
        {
            await UniTask.Create(async () =>
            {
                bool end = false;
                BehaviourStatus lastStatus = BehaviourStatus.InActive;
                while (!end)
                {
                    // await UniTask.NextFrame();
                    await UniTask.Delay(100);
                    var context = unit.BehaviourExecutor.Tick(unit, lastStatus);
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
