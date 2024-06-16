#nullable enable

using System;
using Codice.Client.Commands;

namespace Hedwig.RTSCore.Model.BehaviourTree
{
    [Serializable]
    public class Sequencer : CompositeNode
    {
        public Sequencer(params Node[] children) : base(children)
        {
        }

        public Sequencer()
        { }

        public override BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit)
        {
            foreach (var child in _chidren)
            {
                if(LastRunning && child.LastSuccess)
                {
                    continue;
                }
                if (child is ActionNode)
                {
                    context.SetLastActionNode(child);
                }
                var status = child.Execute(context, unit);
                switch (status)
                {
                    case BehaviourStatus.Failure:
                    case BehaviourStatus.Running:
                        return status;
                    default:
                        break;
                }
            }
            return BehaviourStatus.Success;
        }

        public override void DoReset() { }
    }
}
