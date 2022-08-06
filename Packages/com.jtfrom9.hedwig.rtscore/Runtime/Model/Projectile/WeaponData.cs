#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hedwig.RTSCore
{
    [CreateAssetMenu(menuName = "Hedwig/Projectile/WeaponData", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField]
        public HitType hitType;

        [SerializeField]
        public float power = 0;

        [SerializeField]
        public int attack = 0;
    }
}
