#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UniRx;
using UnityExtensions;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    [CreateAssetMenu(menuName = "Hedwig/Projectile/Projectile", fileName = "Projectile")]
    public class ProjectileObject : ScriptableObject, IProjectileData, IProjectileFactory
    {
        [SerializeField, SearchContext("t:prefab Projectile")]
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
            if (prefab != null)
            {
                return Instantiate(prefab).GetComponent<IProjectileController>();
            }
            return null;
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

        public IProjectileFactory Factory { get => this; }
        #endregion

        #region IProjectileFactory
        public IObservable<IProjectile> OnCreated { get => onCreated; }

        public IProjectile? Create(Vector3 start)
        {
            if (prefab == null) return null;
            IProjectile? projectile = null;

            var projectileController = createController();
            if (projectileController != null)
            {
                projectileController.Initialize(this.name, start);
                projectile = new ProjectileImpl(projectileController, this);
                onCreated.OnNext(projectile);
            }
            return projectile;
        }
        #endregion
    }
}