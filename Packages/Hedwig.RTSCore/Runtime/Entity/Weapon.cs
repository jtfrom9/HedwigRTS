#nullable enable

namespace Hedwig.RTSCore
{
    public interface IWeaponData
    {
        HitType HitType { get; }
        float Power { get; }
        int Attack { get; }
    }
}