#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class UnitManagerImpl: IUnitManager, IUnitCallback
    {
        readonly ReactiveCollection<IUnit> _units = new();
        readonly CompositeDisposable _disposable = new();

        readonly IUnitAttackedEffectFactory _attackedEffectFactory;
        readonly ITargetVisualizerFactory _targetVisualizersFactory;

        void playHitTransformEffect(IUnit unit, IHitObject? hitObject, in DamageEvent e)
        {
            if (hitObject != null && e.ActualDamage > 0)
            {
                unit.Controller.Knockback(hitObject.Direction, hitObject.Power);
            }
        }

        void playHitVisualEffect(IUnit unit, IHitObject? hitObject, in DamageEvent e)
        {
            var effects = _attackedEffectFactory.CreateAttackedEffects(unit, hitObject, in e);
            foreach (var effect in effects)
            {
                effect?.PlayAndDispose().Forget();
            }
        }

        void onUnitAttacked(IUnit unit, IHitObject? hitObject, in DamageEvent damageEvent)
        {
            playHitVisualEffect(unit, hitObject, damageEvent);
            playHitTransformEffect(unit, hitObject, damageEvent);
        }

        async void onUnitDeath(IUnit unit)
        {
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
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

        IUnit? addUnitWithDefaultObject(IUnitController unitController, IUnitData unitObject)
        {
            var unit = new UnitImpl(this, unitObject, unitController, this);
            unitController.Initialize(unit, null, name: null);
            addUnit(unit);
            return unit;
        }

        #region IUnitManager
        IReadOnlyReactiveCollection<IUnit> IUnitManager.Units { get => _units; }

        IUnit IUnitManager.Spawn(IUnitFactory unitFactory, Vector3 position, string? name)
        {
            var unit = unitFactory.Create(this, this, position, name);
            if (unit == null)
            {
                throw new InvalidCastException("fail to spwawn");
            }
            addUnit(unit);
            return unit;
        }

        void IUnitManager.Initialize(IUnitData unitData)
        {
            var unitControllerRepository = ControllerBase.Find<IUnitControllerRepository>();
            if (unitControllerRepository != null)
            {
                foreach (var unitController in unitControllerRepository.GetUnitControllers())
                {
                    addUnitWithDefaultObject(unitController, unitData);
                }
            }
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            this._disposable.Dispose();
        }
        #endregion


        #region IUnitCallback
        void IUnitCallback.OnAttacked(IUnit unit, IHitObject? hitObject, in DamageEvent damageEvent)
            => onUnitAttacked(unit, hitObject, damageEvent);

        void IUnitCallback.OnDeath(IUnit unit)
            => onUnitDeath(unit);
        #endregion

        // ctor
        public UnitManagerImpl(IUnitAttackedEffectFactory attackedEffectFactory, ITargetVisualizerFactory targetVisualizersFactory)
        {
            this._attackedEffectFactory = attackedEffectFactory;
            this._targetVisualizersFactory = targetVisualizersFactory;
        }
    }
}