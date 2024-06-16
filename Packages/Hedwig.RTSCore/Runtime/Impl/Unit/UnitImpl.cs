#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Hedwig.RTSCore.Impl
{
    public class UnitImpl : IUnit, IUnitControllerCallback, ISelectable, IUnitActionRunner
    {
        readonly IUnitManager _unitManager;
        readonly string? _name;
        readonly string? _tag;
        readonly ReactiveProperty<UnitStatus> _status = new(UnitStatus.Spawned);
        readonly IUnitData _unitData;
        readonly IUnitBehaviourExecutor _behaviourExecutor;
        readonly IUnitController _unitController;
        readonly IUnitCallback _callback;
        readonly ILauncher? _launcher;
        readonly List<ITargetVisualizer> _visualizers = new List<ITargetVisualizer>();
        readonly ReactiveProperty<bool> _selected = new ReactiveProperty<bool>();
        readonly ReactiveProperty<int> _health;

        int calcDamage(IHitObject hitObject)
        {
            return hitObject.Attack;
        }

        int calcActualDamage(int damage)
        {
            return Math.Max(damage - _unitData.Deffence, 0);
        }

        void applyDamage(int actualDamage)
        {
            this._health.Value -= actualDamage;
            if (this._health.Value < 0) this._health.Value = 0;
            Debug.Log($"{this}: applyDamage: actualDamage={actualDamage}, health={_health}");
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

        void onDying(IHitObject? hitObject)
        {
            _status.Value = UnitStatus.Dying;
            _callback.OnDying(this, hitObject);
        }

        void raiseEvent(IHitObject? hitObject, in DamageEvent damageEvent)
        {
            _callback.OnAttacked(this, hitObject, damageEvent);

            if (_health.Value == 0)
            {
                onDying(hitObject);
            }
        }

        void damaged(int damage)
        {
            doDamage(damage, out DamageEvent damageEvent);
            raiseEvent(null, damageEvent);
        }

        public void ApplyDead()
        {
            _status.Value = UnitStatus.Dead;
        }

        public class UnitActionStateRunningStore : IUnitActionStateExecutorStatus
        {
            public IUnitActionStateExecutor? CurrentState { get; set; }
            public int CountTick { get; set; }
            public int ElapsedMsec { get; set; }

            IUnit? _target = null;
            readonly Action<IUnit?> _onTargetChanged;

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

        readonly ISubject<IUnitActionStateExecutor> _onStateChanged = new Subject<IUnitActionStateExecutor>();
        readonly ISubject<IUnit?> _onStateTargetChanged = new Subject<IUnit?>();
        readonly UnitActionStateRunningStore _state;

        void doAction(int msecToNextTick)
        {
/*
            if (_unitData.StateHolder.States.Count == 0)
            {
                Debug.LogError($"Invalid States Count", context: Controller.Context);
                return;
            }
            _state.CurrentState ??= _unitData.StateHolder.States[0];
            var currentState = _state.CurrentState;
            var next = _state.CurrentState.Execute(this, _state);
            if (next >= 0)
            {
                if (next >= _unitData.StateHolder.States.Count)
                {
                    Debug.LogError($"[{Name}] Invalid State: next={next}", context: Controller.Context);
                    _state.CurrentState = null;
                }
                else
                {
                    _state.CurrentState = _unitData.StateHolder.States[next];
                    _state.CountTick = 0;
                    _state.ElapsedMsec = 0;
                    // Debug.Log($"[{Name}][State] {currentState.Name} -> {state.currentState.Name}");
                    _onStateChanged.OnNext(_state.CurrentState);
                }
            }
            else
            {
                _state.CountTick++;
                _state.ElapsedMsec += msecToNextTick;
                // Debug.Log($"[{Name}][State] {currentState.Name}");
            }*/
        }

        #region IUnitControllerCallback
        void IUnitControllerCallback.OnHit(IHitObject hitObject)
        {
            switch (_status.Value)
            {
                case UnitStatus.Dying:
                case UnitStatus.Dead:
                    return;
            }
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
            _unitController.Dispose();
            foreach (var visualizer in _visualizers)
            {
                visualizer.Dispose();
            }
            _selected.Dispose();
            _health.Dispose();
            _launcher?.Dispose();
        }
        #endregion

        #region ICharactor
        public IReadOnlyReactiveProperty<int> Health { get => _health; }
        #endregion

        #region IUnit
        public string Name { get => _name ?? Controller.Name; }
        public string? Tag { get => _tag; }
        public IReadOnlyReactiveProperty<UnitStatus> Status { get => _status; }
        public IUnitManager Manager { get => _unitManager; }
        public void SetDestination(Vector3 pos) => _unitController.SetDestination(pos);
        public void Stop() => _unitController.Stop();
        public void SetVisibility(bool v)
        {
            _unitController.SetVisibility(v);
            foreach (var visualizer in _visualizers)
            {
                visualizer.SetVisibility(v);
            }
        }

        public IUnitController Controller { get => _unitController; }

        void IUnit.Damaged(int damage) => damaged(damage);
        void IUnit.ResetPos() => _unitController.ResetPos();

        IUnitActionRunner IUnit.ActionRunner { get => this; }
        IUnitBehaviourExecutor IUnit.BehaviourExecutor { get => _behaviourExecutor; }
        ILauncher? IUnit.Launcher { get => _launcher; }
        #endregion

        #region IUnitActionRunner
        void IUnitActionRunner.DoAction(int nextTick) => doAction(nextTick);
        IObservable<IUnitActionStateExecutor> IUnitActionRunner.OnStateChanged { get => _onStateChanged; }
        IObservable<IUnit?> IUnitActionRunner.OnTargetChanged { get => _onStateTargetChanged; }
        #endregion

        #region ITransformProvider
        ITransform ITransformProvider.Transform { get => Controller.Transform; }
        #endregion

        #region IVisualizerTarget
        void IVisualizerTarget.AddVisualizer(ITargetVisualizer targetVisualizer) => _visualizers.Add(targetVisualizer);
        ISelectable IVisualizerTarget.Selectable { get => this; }
        IVisualProperty IVisualizerTarget.VisualProperty { get => Controller.GetProperty(); }
        #endregion

        public override string ToString()
        {
            return $"{Controller.Name}.Impl({_unitData.Name})";
        }

        public UnitImpl(IUnitManager unitManager,
            IUnitData unitData,
            IUnitBehaviourExecutor behaviourExecutor,
            IUnitController unitController,
            IUnitCallback callback,
            string? name = null,
            string? tag = null,
            ILauncher? launcher = null)
        {
            this._unitManager = unitManager;
            this._name = name;
            this._unitData = unitData;
            this._behaviourExecutor = behaviourExecutor;
            this._unitController = unitController;
            this._callback = callback;
            this._launcher = launcher;
            this._health = new ReactiveProperty<int>(unitData.MaxHealth);
            this._state = new UnitActionStateRunningStore(onTargetChanged: (target) => _onStateTargetChanged.OnNext(target));
        }
    }
}