#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UnityEditor;

using System;
using System.Linq;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/UnitAction/Action", fileName = "UnitAction")]
    public class UnitActionObject : ScriptableObject, IUnitActionStateHolder
    {
        [SerializeReference, SubclassSelector]
        List<IUnitActionStateExecutor> states;

        IReadOnlyList<IUnitActionStateExecutor> IUnitActionStateHolder.States { get => states; }
    }

    [Serializable]
    public class IdleState : IUnitActionStateExecutor
    {
        [SerializeField] int msec;
        [SerializeField] int nextIndex;

        public string Name { get => "Idle"; }

        public int Execute(IUnit unit, IUnitActionStateExecutorStatus state)
        {
            if (state.ElapsedMsec > msec)
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
    public class ApproachState : IUnitActionStateExecutor
    {
        [SerializeField] float distance;
        [SerializeField] int notFoundIndex;
        [SerializeField] int onReachedNextIndex;

        public string Name { get => "Approach"; }

        public int Execute(IUnit unit, IUnitActionStateExecutorStatus state)
        {
            if (state.Target != null)
            {
                var dist = Vector3.Distance(state.Target.Transform.Position, unit.Transform.Position);
                if (dist <= distance)
                {
                    unit.Stop();
                    return onReachedNextIndex;
                }
            }
            var target = unit.Manager.ChoiceOne(unit);
            if (target == null)
            {
                return notFoundIndex;
            }
            state.Target = target;
            unit.SetDestination(target.Transform.Position);
            return -1;
        }
    }

    [Serializable]
    public class AttackAction : IUnitActionStateExecutor
    {
        [SerializeField] ProjectileObject? projectile;
        [SerializeField] bool keepTarget = true;
        [SerializeField] int nextIndex;

        public string Name { get => "Attack"; }

        public int Execute(IUnit unit, IUnitActionStateExecutorStatus state)
        {
            var launcher = unit.Launcher;
            Debug.Log($"launcher = {launcher}, target = {state.Target}");
            if (launcher != null && state.Target != null)
            {
                launcher.SetProjectile(projectile);
                launcher.SetTarget(state.Target);

                if (launcher.CanFire.Value)
                {
                    Debug.Log($"Fire: {state.Target.Transform.Position}");
                    launcher.Fire();
                    if (!keepTarget)
                    {
                        state.Target = null;
                    }
                }
            }
            return nextIndex;
        }
    }

    [Serializable]
    public class FindOtherTagAction : IUnitActionStateExecutor
    {
        [SerializeField] float SearchRadius;
        [SerializeField] bool nearest = true;
        [SerializeField] int nextIndex;
        [SerializeField] int notFoundIndex;

        public string Name { get => "FindOtherTag"; }

        public int Execute(IUnit unit, IUnitActionStateExecutorStatus state)
        {
            if (state.Target != null)
            {
                return nextIndex;
            }
            IUnit? target = null;
            float minDist = 0;
            foreach (var other in unit.Manager.Units)
            {
                if (other == unit) continue;
                if (other.Tag == null) continue;
                if (other.Tag == unit.Tag) continue;
                var dist = Vector3.Distance(unit.Transform.Position, other.Transform.Position);
                if (target == null)
                {
                    target = other;
                    minDist = dist;
                }
                else if (minDist > dist)
                {
                    target = other;
                    minDist = dist;
                }
            }
            if (target != null)
            {
                state.Target = target;
                return nextIndex;
            }
            return notFoundIndex;
        }
    }

    [Serializable]
    public class ApproachToTargetAction : IUnitActionStateExecutor
    {
        [SerializeField] float Distance;
        [SerializeField] int nextIndex;
        [SerializeField] int noTargetIndex;

        public string Name { get => "ApproachToTarget"; }
        public int Execute(IUnit unit, IUnitActionStateExecutorStatus state)
        {
            if (state.Target == null) { return noTargetIndex; }
            var dist = Vector3.Distance(unit.Transform.Position, state.Target.Transform.Position);
            if (dist <= Distance)
            {
                unit.Stop();
                return nextIndex;
            }
            unit.SetDestination(state.Target.Transform.Position);
            return -1;
        }
    }
}