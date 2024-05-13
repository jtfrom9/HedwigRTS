#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Hedwig.RTSCore
{
    public class HitTag
    {
        public const string Environment = "Environment";
        public const string Character = "Character";
        public const string Projectile = "Projectile";
    }

    public enum HitType
    {
        Single,
        Range
    }

    public interface IHitObject
    {
        HitType Type { get; }
        int Attack { get; }
        float Power { get; }
        float Speed { get; }
        Vector3 Direction { get; }
        Vector3 Position{ get; }
    }

    public interface IHitHandler
    {
        void OnHit(IHitObject hitObject);
    }
}