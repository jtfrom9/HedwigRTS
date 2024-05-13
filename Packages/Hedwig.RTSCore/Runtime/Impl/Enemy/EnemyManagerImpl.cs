#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class EnemyManagerImpl: IEnemyManager, IEnemyEvent
    {
        ReactiveCollection<IEnemy> _enemies = new ReactiveCollection<IEnemy>();
        CompositeDisposable disposable = new CompositeDisposable();

        IEnemyAttackedEffectFactory attackedEffectFactory;
        ITargetVisualizerFactory targetVisualizersFactory;

        void playHitTransformEffect(IEnemy enemy, IHitObject? hitObject, in DamageEvent e)
        {
            if (hitObject != null && e.ActualDamage > 0)
            {
                enemy.Controller.Knockback(hitObject.Direction, hitObject.Power);
            }
        }

        void playHitVisualEffect(IEnemy enemy, IHitObject? hitObject, in DamageEvent e)
        {
            var effects = attackedEffectFactory.CreateAttackedEffects(enemy, hitObject, in e);
            foreach (var effect in effects)
            {
                effect?.PlayAndDispose().Forget();
            }
        }

        void onEnemyAttacked(IEnemy enemy, IHitObject? hitObject, in DamageEvent damageEvent)
        {
            playHitVisualEffect(enemy, hitObject, damageEvent);
            playHitTransformEffect(enemy, hitObject, damageEvent);
        }

        async void onEnemyDeath(IEnemy enemy)
        {
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
            _enemies.Remove(enemy);
            enemy.Dispose();
        }

        void addEnemy(IEnemy enemy)
        {
            var visualizers = targetVisualizersFactory.CreateTargetVisualizers(enemy);
            foreach(var visualizer in visualizers) {
                enemy.AddVisualizer(visualizer);
            }
            _enemies.Add(enemy);
        }

        IEnemy? addEnemyWithDefaultObject(IEnemyController enemyController, IEnemyData enemyObject)
        {
            var enemy = new EnemyImpl(enemyObject, enemyController, this);
            enemyController.Initialize("", enemy, null);
            addEnemy(enemy);
            return enemy;
        }

        #region IEnemyManager
        IReadOnlyReactiveCollection<IEnemy> IEnemyManager.Enemies { get => _enemies; }

        IEnemy IEnemyManager.Spawn(IEnemyFactory enemyFactory, Vector3 position)
        {
            var enemy = enemyFactory.Create(this, position);
            if (enemy == null)
            {
                throw new InvalidCastException("fail to spwawn");
            }
            addEnemy(enemy);
            return enemy;
        }

        void IEnemyManager.Initialize(IEnemyData defualtEnemyObject)
        {
            var enemyRepository = ControllerBase.Find<IEnemyControllerRepository>();
            if (enemyRepository != null)
            {
                foreach (var enemyController in enemyRepository.GetEnemyController())
                {
                    addEnemyWithDefaultObject(enemyController, defualtEnemyObject);
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
        void IEnemyEvent.OnAttacked(IEnemy enemy, IHitObject? hitObject, in DamageEvent damageEvent)
            => onEnemyAttacked(enemy, hitObject, damageEvent);

        void IEnemyEvent.OnDeath(IEnemy enemy)
            => onEnemyDeath(enemy);
        #endregion

        // ctor
        public EnemyManagerImpl(IEnemyAttackedEffectFactory attackedEffectFactory, ITargetVisualizerFactory targetVisualizersFactory)
        {
            this.attackedEffectFactory = attackedEffectFactory;
            this.targetVisualizersFactory = targetVisualizersFactory;
        }
    }
}