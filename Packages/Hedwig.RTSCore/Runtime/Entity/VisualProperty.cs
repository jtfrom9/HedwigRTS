#nullable enable

namespace Hedwig.RTSCore
{
    public interface IVisualProperty
    {
        float distanceToGround { get; }
        float distanceToHead { get; }
    }
}