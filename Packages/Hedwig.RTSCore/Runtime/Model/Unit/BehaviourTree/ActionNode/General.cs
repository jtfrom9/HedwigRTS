#nullable enable

using System;

namespace Hedwig.RTSCore.Model.BehaviourTree
{
    [Serializable]
    public class GeneralAction : ActionNode
    {
        Func<INodeExecuteContextWriter, IUnit, BehaviourStatus> _func;
        public override BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit)
        {
            return _func.Invoke(context, unit);
        }
        public override void DoReset() { }
        public override Node Clone()
        {
            return new GeneralAction(_func);
        }
        public GeneralAction(Func<INodeExecuteContextWriter, IUnit, BehaviourStatus> action)
        {
            _func = action;
        }
    }
}