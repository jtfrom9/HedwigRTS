#nullable enable

using System;
using System.Collections.Generic;
using Codice.CM.Common.Tree;
using UniRx;

namespace Hedwig.RTSCore.Model.BehaviourTree
{
    class NodeExecuteContext: INodeExecuteContextWriter
    {
        private Node? lastActionNode = null;
        private Dictionary<string, object> _data = new();

        public BehaviourStatus Status { get; set; }

        public IUnitBehaviourNode? LastActionNode { get => lastActionNode; }

        public void SetLastActionNode(Node? actionNode)
        {
            lastActionNode = actionNode;
        }

        public bool TryGet<T>(string key, out T val)
        {
            if (_data.TryGetValue(key, out var v))
            {
                if (v is T vv)
                {
                    val = vv;
                    return true;
                }
            }
#pragma warning disable CS8601 // Null 参照代入の可能性があります。
            val = default;
#pragma warning restore CS8601 // Null 参照代入の可能性があります。
            return false;
        }

        public void Set<T>(string key, T val)
        {
#pragma warning disable CS8601 // Null 参照代入の可能性があります。
            _data[key] = val;
#pragma warning restore CS8601 // Null 参照代入の可能性があります。
        }
    }

    public class Tree : IUnitBehaviourExecutor
    {
        private readonly Node _root;
        private readonly Subject<INodeExecuteContext> _onTickAfter = new();
        private readonly List<Node> _nodeList = new();

        private void scanNodes(CompositeNode cnode)
        {
            foreach (var node in cnode)
            {
                scanNodes(node);
            }
        }

        private void scanNodes(Node node)
        {
            _nodeList.Add(node);
            if (node is CompositeNode cnode) scanNodes(cnode);
        }

        public void Initialize()
        {
            foreach(var node in _nodeList){
                node.DoReset();
            }
        }

        public Tree(Node rootNode)
        {
            _root = rootNode.Clone();
            // _root = rootNode;
            scanNodes(_root);
            Initialize();
        }

        private void ClearStatus()
        {
            foreach(var node in _nodeList) {
                node.ClearStatus();
            }
        }

        INodeExecuteContext IUnitBehaviourExecutor.Tick(IUnit unit, BehaviourStatus lastStatus)
        {
            if (lastStatus != BehaviourStatus.Running)
            {
                ClearStatus();
            }

            var context = new NodeExecuteContext();
            var status = _root.Execute(context, unit);

            context.Status = status;
            _onTickAfter.OnNext(context);
            return context;
        }
    }
}