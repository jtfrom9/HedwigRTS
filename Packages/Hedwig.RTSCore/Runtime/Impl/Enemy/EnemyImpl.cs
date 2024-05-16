#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEditor.PackageManager;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class EnemyImpl : IEnemy, IEnemyControllerEvent, ISelectable
    {
        IEnemyManager enemyManager;
        string? _name;
        IEnemyData enemyData;
        IEnemyController enemyController;
        IEnemyEvent enemyEvent;
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

        UnitActionStateRunningStore state = new UnitActionStateRunningStore();
        ISubject<UnitActionStateRunningStore> _onStateChanged = new Subject<UnitActionStateRunningStore>();

        void doAction(int msecToNextTick)
        {
            if (enemyData.StateHolder.States.Count == 0)
            {
                Debug.LogError($"Invalid States Count", context: Controller.Context);
                return;
            }
            if (state.currentState == null)
            {
                state.currentState = enemyData.StateHolder.States[0];
            }
            var currentState = state.currentState;
            var next = state.currentState.Execute(this, state);
            if (next >= 0)
            {
                if (next >= enemyData.StateHolder.States.Count)
                {
                    Debug.LogError($"[{Name}] Invalid State: next={next}", context: Controller.Context);
                    state.currentState = null;
                }
                else
                {
                    state.currentState = enemyData.StateHolder.States[next];
                    state.countTick = 0;
                    state.elapsedMsec = 0;
                    // Debug.Log($"[{Name}][State] {currentState.Name} -> {state.currentState.Name}");
                    _onStateChanged.OnNext(state);
                }
            }
            else
            {
                state.countTick++;
                state.elapsedMsec += msecToNextTick;
                // Debug.Log($"[{Name}][State] {currentState.Name}");
            }
        }

        #region IEnemyControllerEvent
        void IEnemyControllerEvent.OnHit(IHitObject hitObject)
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
        public IEnemyManager Manager { get => enemyManager; }
        public void SetDestination(Vector3 pos) => enemyController.SetDestination(pos);
        public void Stop() => enemyController.Stop();

        public IEnemyController Controller { get => enemyController; }

        void IEnemy.Damaged(int damage) => damaged(damage);
        void IEnemy.ResetPos() => enemyController.ResetPos();
        #endregion

        #region IUnit
        void IUnit.DoAction(int nextTick) => doAction(nextTick);
        IObservable<UnitActionStateRunningStore> IUnit.OnStateChanged { get => _onStateChanged; }
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

        public EnemyImpl(IEnemyManager enemyManager, IEnemyData enemyData, IEnemyController enemyController, IEnemyEvent enemyEvent, string? name = null)
        {
            this.enemyManager = enemyManager;
            this._name = name;
            this.enemyData = enemyData;
            this.enemyController = enemyController;
            this.enemyEvent = enemyEvent;
            this.health = new ReactiveProperty<int>(enemyData.MaxHealth);
            this.Controller.SeDebugUnit(this);
        }
    }
}