#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    public struct DamageEvent
    {
        public readonly int damage;
        public readonly int actualDamage;

        public DamageEvent(int damage, int actualDamage = 0)
        {
            this.damage = damage;
            this.actualDamage = actualDamage;
        }
    }

    public interface IEnemyController: ITransformProvider
    {
        void Initialize(string name, IEnemyControllerEvent controllerEvent, Vector3? position);

        string name { get; }
        void SetDestination(Vector3 pos);
        void Stop();
        IVisualProperty GetProperty();

        void ResetPos(); // to bedeelted
        void Knockback(Vector3 direction, float power);
    }

    public interface IEnemyControllerEvent
    {
        void OnHit(IHitObject hitObject);
    }

    public interface IEnemy : IDisposable, ICharactor, IVisualizerTarget
    {
        void SetDestination(Vector3 pos);
        void Stop();

        IEnemyController controller { get; }

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

    public interface IEnemyManager : IDisposable
    {
        IReadOnlyReactiveCollection<IEnemy> Enemies { get; }
        IEnemy Spawn(EnemyObject enemyObject, Vector3 position);

        void Initialize(EnemyObject defaultEnemyObject);
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
    }
}
