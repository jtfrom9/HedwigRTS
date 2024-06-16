#nullable enable

using System;

namespace Hedwig.RTSCore.Model.BehaviourTree
{
    [Serializable]
    public class Selector : CompositeNode
    {
        public Selector(params Node[] children) : base(children)
        {
        }

        public Selector()
        {
        }

        public override BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit)
        {
            foreach (var child in _chidren)
            {
                if (child is ActionNode)
                {
                    context.SetLastActionNode(child);
                }
                var status = child.Execute(context, unit);
                switch (status)
                {
                    case BehaviourStatus.Success:
                    case BehaviourStatus.Running:
                        return status;
                    default:
                        break;
                }
            }
            return BehaviourStatus.Failure;
        }

        public override void DoReset() { }
    }
}