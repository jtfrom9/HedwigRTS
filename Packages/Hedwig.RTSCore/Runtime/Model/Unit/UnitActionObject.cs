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
                    // state.Target = null;
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
    public class RandomMoveAction : IUnitActionStateExecutor
    {
        [SerializeField] Vector2 Min = Vector2.zero;
        [SerializeField] Vector2 Max = Vector2.one;
        [SerializeField] int msec;
        [SerializeField] int nextIndex;

        public string Name { get => "Random"; }

        public int Execute(IUnit unit, IUnitActionStateExecutorStatus state)
        {
            if (state.ElapsedMsec > msec)
            {
                return nextIndex;
            }
            var x = UnityEngine.Random.Range(Min.x, Max.y);
            var z = UnityEngine.Random.Range(Max.x, Max.y);
            unit.SetDestination(new Vector3(x, 0, z));
            return -1;
        }
    }

    [Serializable]
    public class AttackAction : IUnitActionStateExecutor
    {
        [SerializeField] ProjectileObject? projectile;
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
                    state.Target = null;
                }
            }
            return nextIndex;
        }
    }
}