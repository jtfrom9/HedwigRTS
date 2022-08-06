#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Hedwig.RTSCore
{
    using Projectile;

    public class ProjectileImpl : IProjectile, IHitObject
    {
        IProjectileController projectileController;
        ProjectileObject projectileObject;
        ProjectileOption option = new ProjectileOption();
        TrajectoryMap? map = null;

        EndReason endReason = EndReason.Expired;
        CompositeDisposable disposables = new CompositeDisposable();
        Subject<Unit> onStarted = new Subject<Unit>();
        Subject<Unit> onEnded = new Subject<Unit>();
        Subject<Unit> onDestroy = new Subject<Unit>();

        Vector3 toSpearPoint(Vector3 from, Vector3 to, float range) {
            return from + ((to - from).normalized * range);
        }

        Vector3 toPoint(TrajectoryLineMap line, float range, bool spear)
        {
            if (spear)
            {
                var (from, to) = line.GetPoints();
                return toSpearPoint(from, to, range);
            }
            else
            {
                return line.GetToPoint();
            }
        }

        async UniTask<bool> curveMainLoop(TrajectorySectionMap section)
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

        async UniTask<bool> homingMainLoop(TrajectorySectionMap section, ITransform target)
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
                    if (section.adjustMaxAngle < angle)
                    {
                        var cross = Vector3.Cross(dir, prevDir);
                        var length = dir.magnitude;
                        dir = Quaternion.AngleAxis(-section.adjustMaxAngle, cross) * prevDir;
                        to = from + dir.normalized * length;
                    }
                }
                var exitLoop = await projectileController.Move(to, line.GetAccelatedSpeed());
                if (exitLoop)
                    return true;
                section.AddDynamic(new TrajectoryLineMap(section, index, line.fromFactor, line.toFactor));
                from = to; // update next 'from' position
                prevDir = dir;
            }
            return false;
        }

        async UniTask mainLoop(ProjectileObject projectileObject, ITransform target)
        {
            var globalFromPoint = projectileController.transform.Position;
            var globalToPoint = target.Position + target.ShakeRandom(projectileObject.shake);

            if (projectileObject.trajectory == null) {
                //
                // linear Move if no trajectory
                //
                await projectileController.Move(
                    toSpearPoint(globalFromPoint, globalToPoint, projectileObject.range),
                    projectileObject.baseSpeed);
            }
            else
            {
                map = projectileObject.trajectory.ToMap(globalFromPoint, globalToPoint, projectileObject.baseSpeed);
                var sections = map.Sections.ToList();

                //
                // linear Move if only one line and Fire style projectile
                //
                if(sections.Count==1 && !sections[0].IsCurve && projectileObject.type==ProjectileType.Fire) {
                    var section = sections[0];
                    await projectileController.Move(
                        toSpearPoint(globalFromPoint, globalToPoint, projectileObject.range),
                        section.speed);
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
            await projectileController.LastMove(projectileObject.baseSpeed);
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

        async UniTaskVoid start(ProjectileObject projectileObject, ITransform target)
        {
            onStarted.OnNext(Unit.Default);
            await mainLoop(projectileObject, target);
            if (option.DestroyAtEnd)
            {
                dispose();
            }
            onEnded.OnNext(Unit.Default);
        }

        #region IProjectile
        IProjectileController IProjectile.controller { get => projectileController; }
        EndReason IProjectile.EndReason { get => endReason; }

        IObservable<Unit> IProjectile.OnStarted { get => onStarted; }
        IObservable<Unit> IProjectile.OnEnded { get => onEnded; }
        IObservable<Unit> IProjectile.OnDestroy { get => onDestroy; }
        TrajectoryMap? IProjectile.trajectoryMap { get => map; }

        void IProjectile.Start(ITransform target, in ProjectileOption? option)
        {
            if (option != null) this.option = option.Value;
            start(projectileObject, target).Forget();
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            endReason = EndReason.Disposed;
            dispose();
        }
        #endregion

        public override string ToString()
        {
            return $"{projectileController.name}.Impl({endReason})";
        }

        #region IHitObject
        HitType IHitObject.type {
            get
            {
                switch (projectileObject.type)
                {
                    case ProjectileType.Grenade:
                        return HitType.Range;
                    default:
                        return HitType.Single;
                }
            }
        }
        int IHitObject.attack { get => projectileObject?.weaponData?.attack ?? 0; }
        float IHitObject.power { get => projectileObject?.weaponData?.power ?? 0; }
        float _speed;
        float IHitObject.speed { get => _speed; }
        Vector3 IHitObject.direction { get => projectileController.transform.Forward; }
        Vector3 IHitObject.position { get => projectileController.transform.Position; }
        #endregion

        void onHit(in Projectile.EventArg e)
        {
            if (e.collider != null)
            {
                var hitHandler = e.collider.GetComponent<IHitHandler>();
                if (hitHandler != null)
                {
                    if (e.endReason.HasValue && e.endReason.Value == EndReason.CharactorHit)
                    {
                        var transform = projectileController.transform;
                        var speed = e.speed!.Value;
                        Debug.DrawLine(transform.Position,
                            transform.Position + transform.Forward * speed,
                            Color.red, 1f);

                        Debug.DrawLine(transform.Position,
                            transform.Position - transform.Forward * speed,
                            Color.green, 1f);
                    }
                    _speed = e.speed!.Value;
                    hitHandler.OnHit(this);
                }
            }
        }

        public ProjectileImpl(IProjectileController projectileController, ProjectileObject projectileObject)
        {
            this.projectileController = projectileController;
            this.projectileObject = projectileObject;

            projectileController.OnEvent.Subscribe(e =>
            {
                switch (e.type)
                {
                    case Projectile.EventType.Trigger:
                        if (e.endReason.HasValue)
                            this.endReason = e.endReason.Value;
                        onHit(e);
                        break;
                    case Projectile.EventType.Destroy:
                        destroy();
                        break;
                }
            }).AddTo(disposables);
        }
    }
}