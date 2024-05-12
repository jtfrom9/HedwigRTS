#nullable enable

namespace Hedwig.RTSCore
{
    public interface IEnvironmentController: ITransformProvider
    {
        void Initialize(IEnvironmentEvent environmentEvent);
        string name { get; }
    }

    public interface IEnvironmentFactory
    {
        IEnvironment? Create();
    }

    public interface IEnvironmentEvent
    {
        void OnHit(IHitObject hitObject);
    }

    public interface IEnvironment
    {
        IEnvironmentController controller { get; }
    }
}