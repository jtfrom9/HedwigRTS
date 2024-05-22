#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEditor.PackageManager;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class UnitImpl : IUnit, IUnitControllerCallback, ISelectable, IUnitActionRunner
    {
        IUnitManager enemyManager;
        string? _name;
        IUnitData enemyData;
        IUnitController enemyController;
        IUnitCallback enemyEvent;
        int actionState = 0;
        List<ITargetVisualizer> visualizers = new List<ITargetVisualizer>();
        ReactiveProperty<bool> _selected = new ReactiveProperty<bool>();

        ReactiveProperty<int> health;

        int calcDamage(IHitObject hitObject)
        {
            return hitObject.Attack;
        }

        int calcActualDamage(int damage)
        {
            return Math.Max(damage - enemyData.Deffence, 0);
        }

        void applyDamage(int actualDamage)
        {
            this.health.Value -= actualDamage;
            if (this.health.Value < 0) this.health.Value = 0;
            Debug.Log($"{this}: applyDamage: actualDamage={actualDamage}, health={health}");
        }

        void doDamage(int damage, out DamageEvent damageEvent)
        {
            var actualDamage = calcActualDamage(damage);
            applyDamage(actualDamage);
            damageEvent = new DamageEvent(damage, actualDamage: actualDamage);
        }

        void doDamage(IHitObject hitObject, out DamageEvent damageEvent)
        {
            var damage = calcDamage(hitObject);
            doDamage(damage, out damageEvent);
        }

        void raiseEvent(IHitObject? hitObject, in DamageEvent damageEvent)
        {
            enemyEvent.OnAttacked(this, hitObject, damageEvent);

            if (health.Value == 0)
            {
                enemyEvent.OnDeath(this);
            }
        }

        void damaged(int damage)
        {
            doDamage(damage, out DamageEvent damageEvent);
            raiseEvent(null, damageEvent);
        }

        public class UnitActionStateRunningStore : IUnitActionStateExecutorStatus
        {
            public IUnitActionStateExecutor? CurrentState { get; set; }
            public int CountTick { get; set; }
            public int ElapsedMsec { get; set; }

            IUnit? _target = null;
            Action<IUnit?> _onTargetChanged;

            public IUnit? Target
            {
                get => _target;
                set
                {
                    if (_target != value)
                    {
                        _onTargetChanged.Invoke(value);
                    }
                    _target = value;
                }
            }

            public UnitActionStateRunningStore(Action<IUnit?> onTargetChanged)
            {
                _onTargetChanged = onTargetChanged;
            }
        }

        ISubject<IUnitActionStateExecutor> _onStateChanged = new Subject<IUnitActionStateExecutor>();
        ISubject<IUnit?> _onStateTargetChanged = new Subject<IUnit?>();

        UnitActionStateRunningStore state;

        void doAction(int msecToNextTick)
        {
            if (enemyData.StateHolder.States.Count == 0)
            {
                Debug.LogError($"Invalid States Count", context: Controller.Context);
                return;
            }
            if (state.CurrentState == null)
            {
                state.CurrentState = enemyData.StateHolder.States[0];
            }
            var currentState = state.CurrentState;
            var next = state.CurrentState.Execute(this, state);
            if (next >= 0)
            {
                if (next >= enemyData.StateHolder.States.Count)
                {
                    Debug.LogError($"[{Name}] Invalid State: next={next}", context: Controller.Context);
                    state.CurrentState = null;
                }
                else
                {
                    state.CurrentState = enemyData.StateHolder.States[next];
                    state.CountTick = 0;
                    state.ElapsedMsec = 0;
                    // Debug.Log($"[{Name}][State] {currentState.Name} -> {state.currentState.Name}");
                    _onStateChanged.OnNext(state.CurrentState);
                }
            }
            else
            {
                state.CountTick++;
                state.ElapsedMsec += msecToNextTick;
                // Debug.Log($"[{Name}][State] {currentState.Name}");
            }
        }

        #region IEnemyControllerEvent
        void IUnitControllerCallback.OnHit(IHitObject hitObject)
        {
            doDamage(hitObject, out DamageEvent damageEvent);
            raiseEvent(hitObject, damageEvent);
        }
        #endregion

        #region ISelectable
        void ISelectable.Select(bool v) { _selected.Value = v; }
        IReadOnlyReactiveProperty<bool> ISelectable.Selected { get => _selected; }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            enemyController.Dispose();
            foreach (var visualizer in visualizers)
            {
                visualizer.Dispose();
            }
        }
        #endregion

        #region ICharactor
        public IReadOnlyReactiveProperty<int> Health { get => health; }
        #endregion

        #region IEnemy
        public string Name { get => _name ?? Controller.Name; }
        public IUnitManager Manager { get => enemyManager; }
        public void SetDestination(Vector3 pos) => enemyController.SetDestination(pos);
        public void Stop() => enemyController.Stop();

        public IUnitController Controller { get => enemyController; }

        void IUnit.Damaged(int damage) => damaged(damage);
        void IUnit.ResetPos() => enemyController.ResetPos();

        public IUnitActionRunner ActionRunner { get => this; }
        #endregion

        #region IUnit
        void IUnitActionRunner.DoAction(int nextTick) => doAction(nextTick);
        IObservable<IUnitActionStateExecutor> IUnitActionRunner.OnStateChanged { get => _onStateChanged; }
        IObservable<IUnit?> IUnitActionRunner.OnTargetChanged { get => _onStateTargetChanged; }
        #endregion

        #region ITransformProvider
        ITransform ITransformProvider.Transform { get => Controller.Transform; }
        #endregion

        #region IVisualizerTarget
        void IVisualizerTarget.AddVisualizer(ITargetVisualizer targetVisualizer) => visualizers.Add(targetVisualizer);
        ISelectable IVisualizerTarget.Selectable { get => this; }
        IVisualProperty IVisualizerTarget.VisualProperty { get => Controller.GetProperty(); }
        #endregion

        public override string ToString()
        {
            return $"{Controller.Name}.Impl({enemyData.Name})";
        }

        public UnitImpl(IUnitManager enemyManager, IUnitData enemyData, IUnitController enemyController, IUnitCallback enemyEvent, string? name = null)
        {
            this.enemyManager = enemyManager;
            this._name = name;
            this.enemyData = enemyData;
            this.enemyController = enemyController;
            this.enemyEvent = enemyEvent;
            this.health = new ReactiveProperty<int>(enemyData.MaxHealth);
            this.Controller.SeDebugUnit(this);

            this.state = new UnitActionStateRunningStore(onTargetChanged: (target) => _onStateTargetChanged.OnNext(target));
        }
    }
}