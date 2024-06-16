#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;

using UnityExtensions;
using NaughtyAttributes;

using Hedwig.RTSCore.Model.UnitBehaviourTree;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/Behaviour", fileName = "UnitBehaviour")]
    public class UnitBehaviourObject : ScriptableObject, IUnitBehaviourExecutorFactory
    {
        [SerializeReference, SubclassSelector]
        public List<Node> rootNodes;

        public virtual IUnitBehaviourExecutor Create()
        {
            return new UnitBehaviourTree.Tree(rootNodes.Count > 1 ? new Selector(rootNodes.ToArray()) : rootNodes[0]);
        }
    }
}
