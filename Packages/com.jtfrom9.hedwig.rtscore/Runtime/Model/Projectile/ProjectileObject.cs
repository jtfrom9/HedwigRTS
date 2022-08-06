#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Search;
using UniRx;
using UnityExtensions;

namespace Hedwig.RTSCore
{
    public enum ProjectileType
    {
        Fire,
        Burst,
        Grenade
    };

    [CreateAssetMenu(menuName = "Hedwig/Projectile/Projectile", fileName = "Projectile")]
    public class ProjectileObject : ScriptableObject, IProjectileFactory
    {
        [SerializeField, SearchContext("t:prefab Projectile")]
        GameObject? prefab;

        [SerializeField] public ProjectileType type;
        [SerializeField] public bool chargable;

        [SerializeField]
        [Range(0, 256)]
        public int successionCount = 1;

        [SerializeField]
        [Range(10, 1000)]
        public int successionInterval = 0;

        [SerializeField]
        [Range(10, 10000)]
        public int recastTime = 500;

        [SerializeField]
        [Range(0, 2.0f)]
        public float shake = 0f;

        [SerializeField]
        [Min(1)]
        public float baseSpeed = 10f;

        [SerializeField]
        [Min(1)]
        public float range = 10;

        [SerializeField, InspectInline] public Trajectory? trajectory;

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
    }
}