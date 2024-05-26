#nullable enable

using UniRx;

namespace Hedwig.RTSCore
{
    public interface ITimeManager
    {
        IReadOnlyReactiveProperty<bool> Paused { get; }
    }
}
