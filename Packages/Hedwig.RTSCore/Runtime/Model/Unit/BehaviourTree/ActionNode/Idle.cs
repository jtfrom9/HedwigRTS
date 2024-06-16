#nullable enable

using System;
using UnityEngine;

namespace Hedwig.RTSCore.Model.BehaviourTree
{
    [Serializable]
    public class IdleNode : ActionNode
    {
        [SerializeField]
        float _msec;

        float _firstTime = -1;

        public override BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit)
        {
            Debug.Log($"Idle: {Time.time}, diff: {Time.time - _firstTime}");
            if (_firstTime < 0)
            {
                _firstTime = Time.time;
            }
            if (Time.time - _firstTime > _msec / 1000)
            {
                return BehaviourStatus.Success;
            }
            else
            {
                return BehaviourStatus.Running;
            }
        }

        public override void DoReset()
        {
            _firstTime = -1;
        }

        public override Node Clone()
        {
            return new IdleNode(_msec);
        }

        public IdleNode(float msec)
        {
            _msec = msec;
            _firstTime = -1;
        }

        public IdleNode()
        {
        }
    }
}