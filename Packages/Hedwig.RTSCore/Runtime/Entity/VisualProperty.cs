#nullable enable

namespace Hedwig.RTSCore
{
    public interface IVisualProperty
    {
        float DistanceToGround { get; }
        float DistanceToHead { get; }
    }
}