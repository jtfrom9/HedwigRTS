#nullable enable

using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Hedwig.RTSCore
{
    #region UnitAction
    //
    // UniAction
    //
    public interface IUnitActionStateExecutorStatus
    {
        IUnitActionStateExecutor? CurrentState { get; }
        int CountTick { get; }
        int ElapsedMsec { get; }
        IUnit? Target { get; set; }
    }

    public interface IUnitActionStateExecutor
    {
        string Name { get; }
        int Execute(IUnit unit, IUnitActionStateExecutorStatus state);
    }

    public interface IUnitActionStateHolder
    {
        IReadOnlyList<IUnitActionStateExecutor> States { get; }
    }

    public interface IUnitActionRunner
    {
        void DoAction(int nextTick);
        IObservable<IUnitActionStateExecutor> OnStateChanged { get; }
        IObservable<IUnit?> OnTargetChanged { get; }
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
        void Initialize(IUnitControllerCallback callback, Vector3? position, string? name);

        string Name { get; }
        void SetDestination(Vector3 pos);
        void Stop();
        IVisualProperty GetProperty();

        void ResetPos(); // to bedeelted
        void Knockback(Vector3 direction, float power);

        GameObject Context { get; }

        void SeDebugUnit(IUnitActionRunner unit);

        ILauncherController? LauncherController { get; }
    }

    public interface IUnitControllerRepository
    {
        IUnitController[] GetUnitControllers();
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
        void OnAttacked(IUnit unit, IHitObject? hitObject, in DamageEvent damageEvent);
        void OnDeath(IUnit unit);
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

        ILauncher? Launcher { get; }
    }
    #endregion


    #region UnitData
    public interface IUnitData
    {
        string Name { get; }
        int MaxHealth { get; }
        int Deffence { get; }
        IUnitActionStateHolder StateHolder { get; }
    }
    #endregion

    public delegate IUnit? IUnitFacory(IUnitData unitData, Vector3? position, string? name, IUnitController? unitController);
}
