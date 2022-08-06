#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Hedwig.RTSCore
{
    public class EnemyImpl : IEnemy, IEnemyControllerEvent, ISelectable
    {
        EnemyObject enemyObject;
        IEnemyController enemyController;
        IEnemyEvent enemyEvent;
        List<ITargetVisualizer> visualizers = new List<ITargetVisualizer>();
        ReactiveProperty<bool> _selected = new ReactiveProperty<bool>();

        ReactiveProperty<int> health;

        int calcDamage(IHitObject hitObject) {
            return hitObject.attack;
        }

        int calcActualDamage(int damage)
        {
            return Math.Max(damage - enemyObject.Deffence, 0);
        }

        void applyDamage(int actualDamage)
        {
            this.health.Value -= actualDamage;
            if(this.health.Value <0) this.health.Value = 0;
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

        #region IEnemyControllerEvent
        void IEnemyControllerEvent.OnHit(IHitObject hitObject)
        {
            doDamage(hitObject, out DamageEvent damageEvent);
            raiseEvent(hitObject, damageEvent);
        }
        #endregion

        #region ISelectable
        void ISelectable.Select(bool v) { _selected.Value = v; }
        IReadOnlyReactiveProperty<bool> ISelectable.selected { get => _selected; }
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
        public void SetDestination(Vector3 pos) => enemyController.SetDestination(pos);
        public void Stop() => enemyController.Stop();

        public IEnemyController controller { get => enemyController; }

        void IEnemy.Damaged(int damage) => damaged(damage);
        void IEnemy.ResetPos() => enemyController.ResetPos();
        #endregion

        #region IVisualizerTarget
        void IVisualizerTarget.AddVisualizer(ITargetVisualizer targetVisualizer) => visualizers.Add(targetVisualizer);
        ITransform? IVisualizerTarget.transform { get => controller.transform; }
        ISelectable? IVisualizerTarget.selectable { get => this; }
        IVisualProperty? IVisualizerTarget.property { get => controller.GetProperty(); }
        #endregion

        public override string ToString()
        {
            return $"{controller.name}.Impl({enemyObject.name})";
        }

        public EnemyImpl(EnemyObject enemyObject, IEnemyController enemyController, IEnemyEvent enemyEvent)
        {
            this.enemyObject = enemyObject;
            this.enemyController = enemyController;
            this.enemyEvent = enemyEvent;
            this.health = new ReactiveProperty<int>(enemyObject.MaxHealth);
        }
    }
}