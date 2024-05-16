#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using UniRx;
using Cysharp.Threading.Tasks;
using PlasticGui.WorkspaceWindow;

namespace Hedwig.RTSCore
{
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

    public interface IEnemyController: ITransformProvider
    {
        void Initialize(IEnemyControllerEvent controllerEvent, Vector3? position, string? name);

        string Name { get; }
        void SetDestination(Vector3 pos);
        void Stop();
        IVisualProperty GetProperty();

        void ResetPos(); // to bedeelted
        void Knockback(Vector3 direction, float power);

        GameObject Context{ get; }

        void SeDebugUnit(IUnit unit);
    }

    public interface IEnemyControllerEvent
    {
        void OnHit(IHitObject hitObject);
    }

    public interface IUnit
    {
        void DoAction(int nextTick);
        IObservable<UnitActionStateRunningStore> OnStateChanged { get; }
    }

    public interface IEnemy : IDisposable, ITransformProvider, ICharactor, IVisualizerTarget, IUnit
    {
        string Name { get; }

        IEnemyManager Manager { get; }

        void SetDestination(Vector3 pos);
        void Stop();

        IEnemyController Controller { get; }

        void Damaged(int damange);
        void ResetPos();
    }

    public interface IEnemyEvent
    {
        void OnAttacked(IEnemy enemy, IHitObject? hitObject, in DamageEvent damageEvent);
        void OnDeath(IEnemy enemy);
    }

    public interface IEnemyControllerRepository
    {
        IEnemyController[] GetEnemyController();
    }

    public class UnitActionStateRunningStore
    {
        public IUnitActionState? currentState;
        public int countTick;
        public int elapsedMsec;
        public ITransformProvider? target;
    }

    public interface IUnitActionState
    {
        string Name{ get; }
        int Execute(IEnemy unit, UnitActionStateRunningStore state);
    }

    public interface IUnitActionStateHolder
    {
        IReadOnlyList<IUnitActionState> States{ get; }
    }

    public interface IEnemyData
    {
        string Name { get; }
        int MaxHealth { get; }
        int Attack { get; }
        int Deffence { get; }
        IUnitActionStateHolder StateHolder { get; }
    }

    public interface IEnemyFactory
    {
        IEnemy? Create(IEnemyManager manager, IEnemyEvent enemyEvent, Vector3? position, string? name);
    }

    public interface IEnemyManager : IDisposable
    {
        IReadOnlyReactiveCollection<IEnemy> Enemies { get; }
        IEnemy Spawn(IEnemyFactory enemyFactory, Vector3 position, string? name = null);

        void Initialize(IEnemyData defaultEnemyObject);
    }

    public static class EnemyManagerExtension
    {
        public static UniTask RandomWalk(this IEnemyManager manager, float min, float max, int msec, CancellationToken token)
        {
            return UniTask.Create(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var enemy in manager.Enemies)
                    {
                        var x = UnityEngine.Random.Range(min, max);
                        var z = UnityEngine.Random.Range(min, max);
                        var pos = new Vector3(x, 0, z);
                        enemy.SetDestination(pos);
                    }
                    try
                    {
                        await UniTask.Delay(msec, cancellationToken: token);
                    }catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                // manager.StopAll();
            });
        }

        public static void StopAll(this IEnemyManager manager)
        {
            foreach(var enemy in manager.Enemies) {
                enemy.Stop();
            }
        }

        public static IEnemy? ChoiceOne(this IEnemyManager manager, IEnemy self)
        {
            foreach(var enemy in manager.Enemies) {
                if (enemy != self)
                {
                    return enemy;
                }
            }
            return null;
        }
    }
}
