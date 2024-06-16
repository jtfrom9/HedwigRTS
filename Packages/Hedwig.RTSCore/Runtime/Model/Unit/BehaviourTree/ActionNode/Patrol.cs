#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hedwig.RTSCore.Model.UnitBehaviourTree
{
    [Serializable]
    public class PatrolActionNode : ActionNode
    {
        [SerializeField]
        List<Vector3>? _waypoints;

        int currentIndex = -1;

        public override BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit)
        {
            if (_waypoints == null)
                return BehaviourStatus.Failure;
            if (currentIndex < 0)
            {
                currentIndex = 0;
                unit.SetDestination(_waypoints[currentIndex]);
                return BehaviourStatus.Running;
            }
            var dset = _waypoints[currentIndex];
            var pos = unit.Transform.Position;
            if (Vector3.Distance(unit.Transform.Position.Y(0), dset) < 0.01f)
            {
                currentIndex++;
                if (currentIndex >= _waypoints.Count) {
                    return BehaviourStatus.Success;
                }
                Debug.Log($"NextIndex = {currentIndex}");
                unit.SetDestination(_waypoints[currentIndex]);
            }
            return BehaviourStatus.Running;
        }

        public override void DoReset()
        {
            currentIndex = -1;
        }

        public PatrolActionNode(params Vector3[] waypoints)
        {
            _waypoints = waypoints.ToList();
        }

        public PatrolActionNode()
        {
        }
    }
}