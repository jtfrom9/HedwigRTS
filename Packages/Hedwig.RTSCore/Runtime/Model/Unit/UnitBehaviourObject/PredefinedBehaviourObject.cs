#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using Hedwig.RTSCore.Model.UnitBehaviourTree;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/PredefinedBehaviour", fileName = "PredefinedBehaviour")]
    public class PredefinedBehaviourObject : UnitBehaviourObject
    {
        [SerializeReference, SubclassSelector, Required]
        IPredefinedUnitBehaviour? PredefinedBehaviour;

        public override IUnitBehaviourExecutor Create()
        {
            return PredefinedBehaviour!.Create();
        }
    }

    public interface IPredefinedUnitBehaviour: IUnitBehaviourExecutorFactory
    {
    }

    [Serializable]
    public class TestBehaviour1: IPredefinedUnitBehaviour
    {
        public IUnitBehaviourExecutor Create()
        {
            return new UnitBehaviourTree.Tree(new UnitBehaviourTree.Sequencer(
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
}
