#nullable enable

using System.Collections.Generic;
using UnityEngine;

using Hedwig.RTSCore.Model.BehaviourTree;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/Behaviour", fileName = "UnitBehaviour")]
    public class UnitBehaviourObject : ScriptableObject, IUnitBehaviourExecutorFactory
    {
        [SerializeReference, SubclassSelector]
        public List<Node> rootNodes;

        public virtual IUnitBehaviourExecutor Create()
        {
            return new Hedwig.RTSCore.Model.BehaviourTree.Tree(rootNodes.Count > 1 ? new Selector(rootNodes.ToArray()) : rootNodes[0]);
        }
    }
}
