#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UniRx;
using UnityExtensions;
using VContainer;
using VContainer.Unity;
using NaughtyAttributes;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Projectile/Projectile", fileName = "Projectile")]
    public class ProjectileObject : ScriptableObject, IProjectileData
    {
        [SerializeField, SearchContext("t:prefab Projectile")]
        [Required]
        GameObject? prefab;

        [SerializeField]
        ProjectileType type;

        [SerializeField]
        bool chargable;

        [SerializeField]
        [Range(0, 256)]
        int successionCount = 1;

        [SerializeField]
        [Range(10, 1000)]
        int successionInterval = 0;

        [SerializeField]
        [Range(10, 10000)]
        int recastTime = 500;

        [SerializeField]
        [Range(0, 2.0f)]
        float shake = 0f;

        [SerializeField]
        [Min(1)]
        float baseSpeed = 10f;

        [SerializeField]
        [Min(1)]
        float range = 10;

        [SerializeField, InspectInline] public TrajectoryObject? trajectory;

        [SerializeField, InspectInline] public WeaponData? weaponData;

        private IProjectileController? createController()
        {
            return Instantiate(prefab!).GetComponent<IProjectileController>();
        }

        Subject<IProjectile> onCreated = new Subject<IProjectile>();

        #region IProjectileData
        public string Name { get => name; }
        public ProjectileType Type { get => type; }
        public bool Chargable { get => chargable; }
        public int SuccessionCount { get => successionCount; }
        public int SuccessionInterval { get => successionInterval; }
        public int RecastTime { get => recastTime; }
        public float Shake { get => shake; }
        public float BaseSpeed { get => baseSpeed; }
        public float Range { get => range; }
        public IWeaponData? WeaponData { get => weaponData; }

        public Vector3[] MakePoints(Vector3 from, Vector3 to)
        {
            if (trajectory == null)
            {
                return new Vector3[] { };
            }
            else
            {
                return trajectory.MakePoints(from, to, baseSpeed);
            }
        }

        public bool HasMap { get => trajectory != null; }

        public ITrajectoryMap ToMap(Vector3 from, Vector3 to)
        {
            if (trajectory == null)
            {
                throw new InvalidConditionException("No Trajection");
            }
            return trajectory.ToMap(from, to, baseSpeed);
        }

        #endregion

        public IObservable<IProjectile> OnCreated { get => onCreated; }

        public IProjectile Create(Vector3 start, string? name)
        {
            var projectileController = createController() ?? throw new InvalidConditionException("No ProjectileController");
            var projectile = new ProjectileImpl(projectileController, this);
            projectileController.Initialize(
                projectile,
                initial: start,
                name: name ?? this.name);
            onCreated.OnNext(projectile);
            return projectile;
        }

        public IProjectile CreateProjectileWithResolver(
            IObjectResolver resolver,
            Vector3 start,
            string? name)
        {
            var go = resolver.Instantiate(prefab!);
            if (!go.TryGetComponent<IProjectileController>(out var projectileController)) {
                Destroy(go);
                throw new InvalidConditionException("No IProjectileController");
            }
            resolver.Inject(projectileController);
            var projectile = new ProjectileImpl(projectileController, this);
            projectileController.Initialize(
                    projectile,
                    initial: start,
                    name: name ?? this.name);
            onCreated.OnNext(projectile);
            return projectile;
        }
    }

    public static class ProjectileObjectDIExtension
    {
        public static void RegisterProjectiles(this IContainerBuilder builder, IEnumerable<ProjectileObject> projectileObjects)
        {
            var dict = projectileObjects.ToDictionary(pobj => pobj as IProjectileData, pobj => pobj);

            builder.Register<IProjectileFactory>(resolver => (projectileData, start, name) =>
            {
                if(dict.TryGetValue(projectileData, out var projectileObject))
                {
                    return projectileObject.CreateProjectileWithResolver(resolver, start, name);
                }else {
                    return null;
                }
            }, Lifetime.Transient);
        }
    }
}