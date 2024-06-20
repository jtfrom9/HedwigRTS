#nullable enable

using System;
using UnityEngine;

namespace Hedwig.RTSCore.Model.BehaviourTree
{
    [Serializable]
    public class FindUnitNode : Node
    {
        [SerializeField] float _searchRadius;
        [SerializeField] bool _findNew = true;

        public override BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit)
        {
            if (context.TryGet<IUnit>("Target", out var target) && !_findNew)
            {
                return BehaviourStatus.Failure;
            }
            target = null;
            float minDist = 0;
            foreach (var other in unit.Manager.Units)
            {
                if (other == unit) continue;
                if (other.Tag == null) continue;
                if (other.Tag == unit.Tag) continue;
                var dist = Vector3.Distance(unit.Transform.Position, other.Transform.Position);
                if (target == null)
                {
                    target = other;
                    minDist = dist;
                }
                else if (minDist > dist)
                {
                    target = other;
                    minDist = dist;
                }
            }
            if (target != null)
            {
                context.Set<IUnit>("Target", target);
                return BehaviourStatus.Success;
            }
            return BehaviourStatus.Failure;
        }

        public override void DoReset()
        {
        }

        public override Node Clone()
        {
            return new FindUnitNode();
        }

        public FindUnitNode(float searchRadius, bool findNew)
        {
            _searchRadius = searchRadius;
            _findNew = findNew;
        }

        public FindUnitNode()
        {
        }
    }
}