#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore.Impl
{
    public class ProjectileImpl : IProjectile, IHitObject
    {
        readonly IProjectileController projectileController;
        readonly IProjectileData projectileData;
        ProjectileOption option = new ProjectileOption();
        ITrajectoryMap? map = null;

        ProjectileEndReason endReason = ProjectileEndReason.Expired;
        readonly CompositeDisposable disposables = new CompositeDisposable();
        readonly Subject<Unit> onStarted = new Subject<Unit>();
        readonly Subject<Unit> onEnded = new Subject<Unit>();
        readonly Subject<Unit> onDestroy = new Subject<Unit>();

        Vector3 toSpearPoint(Vector3 from, Vector3 to, float range) {
            return from + ((to - from).normalized * range);
        }

        // Vector3 toPoint(TrajectoryLineMap line, float range, bool spear)
        // {
        //     if (spear)
        //     {
        //         var (from, to) = line.GetPoints();
        //         return toSpearPoint(from, to, range);
        //     }
        //     else
        //     {
        //         return line.GetToPoint();
        //     }
        // }

        async UniTask<bool> curveMainLoop(ITrajectorySectionMap section)
        {
            //
            // Move to the pont which premaild by trajectory setting
            //
            foreach (var line in section.Lines)
            {
                var exitLoop = await projectileController.Move(
                    line.GetToPoint(),
                    line.GetAccelatedSpeed());
                // section.speed);
                if (exitLoop)
                    return true;
            }
            return false;
        }

        async UniTask<bool> homingMainLoop(ITrajectorySectionMap section, ITransform target)
        {
            var prevDir = Vector3.zero;
            var from = section.Lines.First().GetFromPoint();
            var lines = section.Lines.ToArray();
            foreach (var (line, index) in lines.Select((line, index) => (line, index)))
            {
                var to = Vector3.Lerp(from, target.Position, (float)1 / (float)(lines.Length - index));
                var dir = to - from;
                if (!line.IsFirst)
                {
                    var angle = Vector3.Angle(dir, prevDir);
                    if (section.AdjustMaxAngle < angle)
                    {
                        var cross = Vector3.Cross(dir, prevDir);
                        var length = dir.magnitude;
                        dir = Quaternion.AngleAxis(-section.AdjustMaxAngle, cross) * prevDir;
                        to = from + dir.normalized * length;
                    }
                }
                var exitLoop = await projectileController.Move(to, line.GetAccelatedSpeed());
                if (exitLoop)
                    return true;
                section.AddDynamicLine(index, line.FromFactor, line.ToFactor);
                from = to; // update next 'from' position
                prevDir = dir;
            }
            return false;
        }

        async UniTask mainLoop(IProjectileData projectileObject, ITransform target)
        {
            var globalFromPoint = projectileController.Transform.Position;
            var globalToPoint = target.Position + target.ShakeRandom(projectileObject.Shake);

            if (!projectileObject.HasMap) {
                //
                // linear Move if no trajectory
                //
                await projectileController.Move(
                    toSpearPoint(globalFromPoint, globalToPoint, projectileObject.Range),
                    projectileObject.BaseSpeed);
            }
            else
            {
                map = projectileObject.ToMap(globalFromPoint, globalToPoint);
                var sections = map.Sections.ToList();

                //
                // linear Move if only one line and Fire style projectile
                //
                if(sections.Count==1 && !sections[0].IsCurve && projectileObject.Type==ProjectileType.Fire) {
                    var section = sections[0];
                    await projectileController.Move(
                        toSpearPoint(globalFromPoint, globalToPoint, projectileObject.Range),
                        section.Speed);
                }
                else
                {
                    //
                    // mainloop for curved Move
                    //
                    foreach (var section in sections)
                    {
                        var exitLoop = false;
                        if (!section.IsHoming)
                        {
                            exitLoop = await curveMainLoop(section);
                        } else {
                            exitLoop = await homingMainLoop(section, target);
                        }
                        if(exitLoop)
                            break;
                    }
                }
            }

            //
            // last one step move to the object will hit
            //
            await projectileController.LastMove(projectileObject.BaseSpeed);
        }

        void destroy()
        {
            disposables.Clear();
            onStarted.OnCompleted();
            onEnded.OnCompleted();
            onDestroy.OnNext(Unit.Default);
            onDestroy.OnCompleted();
        }

        void dispose()
        {
            projectileController.Dispose();
        }

        async UniTaskVoid start(IProjectileData projectileData, ITransform target)
        {
            onStarted.OnNext(Unit.Default);
            await mainLoop(projectileData, target);
            if (option.DestroyAtEnd)
            {
                dispose();
            }
            onEnded.OnNext(Unit.Default);
        }

        #region IProjectile
        IProjectileController IProjectile.Controller { get => projectileController; }
        ProjectileEndReason IProjectile.EndReason { get => endReason; }

        IObservable<Unit> IProjectile.OnStarted { get => onStarted; }
        IObservable<Unit> IProjectile.OnEnded { get => onEnded; }
        IObservable<Unit> IProjectile.OnDestroy { get => onDestroy; }

        void IProjectile.Start(ITransform target, in ProjectileOption? option)
        {
            if (option != null) this.option = option.Value;
            start(projectileData, target).Forget();
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            endReason = ProjectileEndReason.Disposed;
            dispose();
        }
        #endregion

        public override string ToString()
        {
            return $"{projectileController.Name}.Impl({endReason})";
        }

        #region IHitObject
        HitType IHitObject.Type {
            get
            {
                switch (projectileData.Type)
                {
                    case ProjectileType.Grenade:
                        return HitType.Range;
                    default:
                        return HitType.Single;
                }
            }
        }
        int IHitObject.Attack { get => projectileData?.WeaponData?.Attack ?? 0; }
        float IHitObject.Power { get => projectileData?.WeaponData?.Power ?? 0; }
        float _speed;
        float IHitObject.Speed { get => _speed; }
        Vector3 IHitObject.Direction { get => projectileController.Transform.Forward; }
        Vector3 IHitObject.Position { get => projectileController.Transform.Position; }
        #endregion

        void onHit(in ProjectileEventArg e)
        {
            if (e.Collider != null)
            {
                var hitHandler = e.Collider.GetComponent<IHitHandler>();
                if (hitHandler != null)
                {
                    if (e.EndReason.HasValue && e.EndReason.Value == ProjectileEndReason.CharactorHit)
                    {
                        var transform = projectileController.Transform;
                        var speed = e.Speed!.Value;
                        Debug.DrawLine(transform.Position,
                            transform.Position + transform.Forward * speed,
                            Color.red, 1f);

                        Debug.DrawLine(transform.Position,
                            transform.Position - transform.Forward * speed,
                            Color.green, 1f);
                    }
                    _speed = e.Speed!.Value;
                    hitHandler.OnHit(this);
                }
            }
        }

        public ProjectileImpl(IProjectileController projectileController, IProjectileData projectileData)
        {
            this.projectileController = projectileController;
            this.projectileData = projectileData;

            projectileController.OnEvent.Subscribe(e =>
            {
                switch (e.Type)
                {
                    case ProjectileEventType.Trigger:
                        if (e.EndReason.HasValue)
                            this.endReason = e.EndReason.Value;
                        onHit(e);
                        break;
                    case ProjectileEventType.Destroy:
                        destroy();
                        break;
                }
            }).AddTo(disposables);
        }
    }
}