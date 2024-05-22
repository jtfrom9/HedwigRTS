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
        ReactiveCollection<IUnit> _enemies = new ReactiveCollection<IUnit>();
        CompositeDisposable disposable = new CompositeDisposable();

        IEnemyAttackedEffectFactory attackedEffectFactory;
        ITargetVisualizerFactory targetVisualizersFactory;

        void playHitTransformEffect(IUnit enemy, IHitObject? hitObject, in DamageEvent e)
        {
            if (hitObject != null && e.ActualDamage > 0)
            {
                enemy.Controller.Knockback(hitObject.Direction, hitObject.Power);
            }
        }

        void playHitVisualEffect(IUnit enemy, IHitObject? hitObject, in DamageEvent e)
        {
            var effects = attackedEffectFactory.CreateAttackedEffects(enemy, hitObject, in e);
            foreach (var effect in effects)
            {
                effect?.PlayAndDispose().Forget();
            }
        }

        void onEnemyAttacked(IUnit enemy, IHitObject? hitObject, in DamageEvent damageEvent)
        {
            playHitVisualEffect(enemy, hitObject, damageEvent);
            playHitTransformEffect(enemy, hitObject, damageEvent);
        }

        async void onEnemyDeath(IUnit enemy)
        {
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
            _enemies.Remove(enemy);
            enemy.Dispose();
        }

        void addEnemy(IUnit enemy)
        {
            var visualizers = targetVisualizersFactory.CreateTargetVisualizers(enemy);
            foreach(var visualizer in visualizers) {
                enemy.AddVisualizer(visualizer);
            }
            _enemies.Add(enemy);
        }

        IUnit? addEnemyWithDefaultObject(IUnitController enemyController, IUnitData UnitObject)
        {
            var enemy = new UnitImpl(this, UnitObject, enemyController, this);
            enemyController.Initialize(enemy, null, name: null);
            addEnemy(enemy);
            return enemy;
        }

        #region IEnemyManager
        IReadOnlyReactiveCollection<IUnit> IUnitManager.Enemies { get => _enemies; }

        IUnit IUnitManager.Spawn(IUnitFactory enemyFactory, Vector3 position, string? name)
        {
            var enemy = enemyFactory.Create(this, this, position, name);
            if (enemy == null)
            {
                throw new InvalidCastException("fail to spwawn");
            }
            addEnemy(enemy);
            return enemy;
        }

        void IUnitManager.Initialize(IUnitData defualtUnitObject)
        {
            var enemyRepository = ControllerBase.Find<IEnemyControllerRepository>();
            if (enemyRepository != null)
            {
                foreach (var enemyController in enemyRepository.GetEnemyController())
                {
                    addEnemyWithDefaultObject(enemyController, defualtUnitObject);
                }
            }
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            this.disposable.Dispose();
        }
        #endregion


        #region IEnemyEvent
        void IUnitCallback.OnAttacked(IUnit enemy, IHitObject? hitObject, in DamageEvent damageEvent)
            => onEnemyAttacked(enemy, hitObject, damageEvent);

        void IUnitCallback.OnDeath(IUnit enemy)
            => onEnemyDeath(enemy);
        #endregion

        // ctor
        public UnitManagerImpl(IEnemyAttackedEffectFactory attackedEffectFactory, ITargetVisualizerFactory targetVisualizersFactory)
        {
            this.attackedEffectFactory = attackedEffectFactory;
            this.targetVisualizersFactory = targetVisualizersFactory;
        }
    }
}