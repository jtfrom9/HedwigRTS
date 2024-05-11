#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Projectile/WeaponData", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject, IWeaponData
    {
        [SerializeField]
        HitType hitType;

        [SerializeField]
        float power = 0;

        [SerializeField]
        int attack = 0;

        #region IWeaponData
        public HitType HitType { get => hitType; }
        public float Power { get => power; }
        public int Attack { get => attack; }
        #endregion
    }
}
