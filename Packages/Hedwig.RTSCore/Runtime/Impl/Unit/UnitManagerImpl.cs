#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Hedwig.RTSCore.Impl
{
    public class UnitManagerImpl: IUnitManager, IUnitCallback
    {
        readonly ReactiveCollection<IUnit> _units = new();
        readonly Dictionary<IUnit, UniTask> _attackedEffectTasks = new();
        readonly Dictionary<IUnit, UniTask> _deathEffectTasks = new();
        readonly CompositeDisposable _disposable = new();

        readonly IUnitFacory _unitFactory;
        readonly IUnitAttackedEffectFactory _attackedEffectFactory;
        readonly ITargetVisualizerFactory _targetVisualizersFactory;

        async UniTask playHitTransformEffect(IUnit unit, IHitObject? hitObject, DamageEvent e)
        {
            if (hitObject != null && e.ActualDamage > 0)
            {
                await unit.Controller.Knockback(hitObject.Direction, hitObject.Power);
            }
        }

        async UniTask playHitVisualEffect(IUnit unit, IHitObject? hitObject, DamageEvent e)
        {
            var effects = _attackedEffectFactory.CreateAttackedEffects(unit, hitObject, in e);
            await effects.PlayAndDispose();
        }

        async UniTask playDeathEffect(IUnit unit, IHitObject? hitObject)
        {
            var effects = _attackedEffectFactory.CreateDeathEffects(unit, hitObject);
            await effects.PlayAndDispose();
        }

        async UniTaskVoid onUnitAttacked(IUnit unit, IHitObject? hitObject, DamageEvent damageEvent)
        {
            // skip if successing effects has not done
            if (_attackedEffectTasks.TryGetValue(unit, out var task))
            {
                return;
            }
            var source = new UniTaskCompletionSource();
            _attackedEffectTasks[unit] = source.Task;
            // begin attacked effect
            await UniTask.WhenAll(
                playHitVisualEffect(unit, hitObject, damageEvent),
                playHitTransformEffect(unit, hitObject, damageEvent)
            );
            source.TrySetResult();
            _attackedEffectTasks.Remove(unit);
        }

        async UniTaskVoid onUnitDeath(IUnit unit, IHitObject? hitObject)
        {
            // skip if successing death effects has not done (but this supposeed not to be)
            if (_deathEffectTasks.TryGetValue(unit, out var deathEffectTask)) {
                return;
            }
            // wait first scessing attackedEffect
            if (_attackedEffectTasks.TryGetValue(unit, out var attackedEffectTask))
            {
                await attackedEffectTask;
            }

            // make invisible
            unit.SetVisibility(false);

            // begin death effect
            deathEffectTask = playDeathEffect(unit, hitObject);
            _deathEffectTasks[unit] = deathEffectTask;
            await deathEffectTask;

            if (unit is UnitImpl impl)
            {
                impl.ApplyDead();
            }
            await UniTask.NextFrame();

            _deathEffectTasks.Remove(unit);
            _units.Remove(unit);
            unit.Dispose();
        }

        void addUnit(IUnit unit)
        {
            var visualizers = _targetVisualizersFactory.CreateTargetVisualizers(unit);
            foreach(var visualizer in visualizers) {
                unit.AddVisualizer(visualizer);
            }
            _units.Add(unit);
        }

        #region IUnitManager
        IReadOnlyReactiveCollection<IUnit> IUnitManager.Units { get => _units; }

        IUnit IUnitManager.Spawn(IUnitData unitData, Vector3 position, string? name, string? tag)
        {
            var unit = _unitFactory.Invoke(unitData, position, name, tag, null);
            if (unit == null)
            {
                throw new InvalidCastException("fail to spwawn");
            }
            addUnit(unit);
            return unit;
        }

        void IUnitManager.Register(IUnitController unitController, IUnitData unitData)
        {
            var unit = _unitFactory.Invoke(unitData, position: null, name: null, tag: null, unitController);
            if (unit == null)
            {
                throw new InvalidCastException("fail to spwawn");
            }
            addUnit(unit);
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            this._disposable.Dispose();
        }
        #endregion


        #region IUnitCallback
        void IUnitCallback.OnAttacked(IUnit unit, IHitObject? hitObject, DamageEvent damageEvent)
        {
            onUnitAttacked(unit, hitObject, damageEvent).Forget();
        }

        void IUnitCallback.OnDying(IUnit unit, IHitObject? hitObject)
        {
            onUnitDeath(unit, hitObject).Forget();
        }
        #endregion

        // ctor
        public UnitManagerImpl(IUnitFacory unitFacory,IUnitAttackedEffectFactory attackedEffectFactory, ITargetVisualizerFactory targetVisualizersFactory)
        {
            this._unitFactory = unitFacory;
            this._attackedEffectFactory = attackedEffectFactory;
            this._targetVisualizersFactory = targetVisualizersFactory;
        }
    }
}