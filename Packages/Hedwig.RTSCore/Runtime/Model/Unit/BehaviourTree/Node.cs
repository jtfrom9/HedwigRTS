#nullable enable

using System;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;

namespace Hedwig.RTSCore.Model.UnitBehaviourTree
{
    public interface INodeExecuteContextWriter: INodeExecuteContext
    {
        void SetLastActionNode(Node? lastActionNode);
        void Set<T>(string key, T val);
    }

    [Serializable]
    public abstract class Node: IUnitBehaviourNode
    {
        [SerializeField]
        string? name;

        protected BehaviourStatus _lastStatus = BehaviourStatus.Failure;

        public abstract BehaviourStatus DoExecute(INodeExecuteContextWriter context, IUnit unit);
        public abstract void DoReset();

        public BehaviourStatus Execute(INodeExecuteContextWriter context, IUnit unit)
        {
            var status = DoExecute(context, unit);
            _lastStatus = status;
            if (status == BehaviourStatus.Success)
            {
                DoReset();
            }
            return status;
        }

        public void ClearStatus()
        {
            _lastStatus = BehaviourStatus.InActive;
        }

        public bool LastRunning
        {
            get
            {
                return _lastStatus == BehaviourStatus.Running;
            }
        }

        public bool LastSuccess
        {
            get
            {
                return _lastStatus == BehaviourStatus.Success;
            }
        }

        string IUnitBehaviourNode.Name
        {
            get
            {
                if (name == null)
                {
                    name = GetType().Name;
                }
                return name;
            }
        }

        BehaviourStatus IUnitBehaviourNode.LastStatus
        {
            get => _lastStatus;
        }
    }

    [Serializable]
    public abstract class CompositeNode : Node, IEnumerable<Node>
    {
        [SerializeReference, SubclassSelector]
        protected Node[] _chidren;

        public CompositeNode(params Node[] children)
        {
            _chidren = children;
        }

        public IEnumerator<Node> GetEnumerator()
        {
            foreach (var node in _chidren)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public abstract class ActionNode : Node
    {
    }
}
