#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore
{
    #region UnitAction
    //
    // UniAction
    //
    public class UnitActionStateRunningStore
    {
        public IUnitActionState? currentState;
        public int countTick;
        public int elapsedMsec;
        public ITransformProvider? target;
    }

    public interface IUnitActionState
    {
        string Name { get; }
        int Execute(IUnit unit, UnitActionStateRunningStore state);
    }

    public interface IUnitActionStateHolder
    {
        IReadOnlyList<IUnitActionState> States { get; }
    }

    public interface IUnitActionRunner
    {
        void DoAction(int nextTick);
        IObservable<UnitActionStateRunningStore> OnStateChanged { get; }
    }
    #endregion

    #region UnitController
    //
    // UnitController
    //
    public interface IUnitControllerCallback
    {
        void OnHit(IHitObject hitObject);
    }

    public interface IUnitController : ITransformProvider
    {
        void Initialize(IUnitControllerCallback controllerEvent, Vector3? position, string? name);

        string Name { get; }
        void SetDestination(Vector3 pos);
        void Stop();
        IVisualProperty GetProperty();

        void ResetPos(); // to bedeelted
        void Knockback(Vector3 direction, float power);

        GameObject Context { get; }

        void SeDebugUnit(IUnitActionRunner unit);
    }

    public interface IEnemyControllerRepository
    {
        IUnitController[] GetEnemyController();
    }
    #endregion

    #region Unit
    //
    // Unit
    //
    public struct DamageEvent
    {
        public readonly int Damage;
        public readonly int ActualDamage;

        public DamageEvent(int damage, int actualDamage = 0)
        {
            this.Damage = damage;
            this.ActualDamage = actualDamage;
        }
    }

    public interface IUnitCallback
    {
        void OnAttacked(IUnit enemy, IHitObject? hitObject, in DamageEvent damageEvent);
        void OnDeath(IUnit enemy);
    }

    public interface IUnit : IDisposable, ITransformProvider, ICharactor, IVisualizerTarget
    {
        string Name { get; }

        IUnitManager Manager { get; }

        void SetDestination(Vector3 pos);
        void Stop();

        IUnitController Controller { get; }

        void Damaged(int damange);
        void ResetPos();

        IUnitActionRunner ActionRunner { get; }
    }
    #endregion


    #region UnitData, UnitFactory
    public interface IUnitData
    {
        string Name { get; }
        int MaxHealth { get; }
        int Attack { get; }
        int Deffence { get; }
        IUnitActionStateHolder StateHolder { get; }
    }

    public interface IUnitFactory
    {
        IUnit? Create(IUnitManager manager, IUnitCallback enemyEvent, Vector3? position, string? name);
    }
    #endregion
}
