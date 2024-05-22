#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityEditor;

using System;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/UnitAction/Action", fileName = "UnitAction")]
    public class UnitActionObject : ScriptableObject, IUnitActionStateHolder
    {
        [SerializeReference, SubclassSelector]
        List<IUnitActionState> states;

        IReadOnlyList<IUnitActionState> IUnitActionStateHolder.States { get => states; }
    }

    [Serializable]
    public class IdleState : IUnitActionState
    {
        [SerializeField] int msec;
        [SerializeField] int nextIndex;

        public string Name { get => "Idle"; }

        public int Execute(IUnit unit, UnitActionStateRunningStore state)
        {
            if (state.elapsedMsec > msec)
            {
                return nextIndex;
            }
            else
            {
                return -1;
            }
        }
    }

    [Serializable]
    public class ApproachState : IUnitActionState
    {
        [SerializeField] float distance;
        [SerializeField] int notFoundIndex;
        [SerializeField] int onReachedNextIndex;

        public string Name { get => "Approach"; }

        public int Execute(IUnit unit, UnitActionStateRunningStore state)
        {
            if (state.target != null)
            {
                var dist = Vector3.Distance(state.target.Transform.Position, unit.Transform.Position);
                if (dist <= distance)
                {
                    state.target = null;
                    return onReachedNextIndex;
                }
            }
            var target = unit.Manager.ChoiceOne(unit);
            if (target == null)
            {
                return notFoundIndex;
            }
            state.target = target;
            unit.SetDestination(target.Transform.Position);
            return -1;
        }
    }

    [Serializable]
    public class RandomMoveAction : IUnitActionState
    {
        [SerializeField] Vector2 Min = Vector2.zero;
        [SerializeField] Vector2 Max = Vector2.one;
        [SerializeField] int msec;
        [SerializeField] int nextIndex;

        public string Name { get => "Random"; }

        public int Execute(IUnit unit, RTSCore.UnitActionStateRunningStore state)
        {
            if (state.elapsedMsec > msec)
            {
                return nextIndex;
            }
            var x = UnityEngine.Random.Range(Min.x, Max.y);
            var z = UnityEngine.Random.Range(Max.x, Max.y);
            unit.SetDestination(new Vector3(x, 0, z));
            return -1;
        }
    }
}