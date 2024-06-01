#nullable enable

namespace Hedwig.RTSCore
{
    public interface IEnvironmentEvent
    {
        void OnHit(IHitObject hitObject);
    }

    public interface IEnvironmentController: ITransformProvider
    {
        void Initialize(IEnvironmentEvent environmentEvent);
        string Name { get; }
    }

    public interface IEnvironmentFactory
    {
        IEnvironment Create();
    }

    public interface IEnvironment
    {
        IEnvironmentController Controller { get; }
    }
}