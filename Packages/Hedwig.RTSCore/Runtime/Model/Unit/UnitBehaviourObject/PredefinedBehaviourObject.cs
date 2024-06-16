#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Unit/PredefinedBehaviour", fileName = "PredefinedBehaviour")]
    public class PredefinedBehaviourObject : UnitBehaviourObject
    {
        [SerializeReference, SubclassSelector, Required]
        IPredefinedUnitBehaviour? PredefinedBehaviour;

        public override IUnitBehaviourExecutor Create()
        {
            return PredefinedBehaviour!.Create();
        }
    }

    public interface IPredefinedUnitBehaviour: IUnitBehaviourExecutorFactory
    {
    }
}
