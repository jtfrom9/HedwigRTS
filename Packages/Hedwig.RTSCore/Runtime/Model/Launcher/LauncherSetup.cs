#nullable enable

using VContainer;

using Hedwig.RTSCore.Impl;
using VContainer.Unity;

namespace Hedwig.RTSCore.Model
{
    public static class LauncherDIExtension
    {
        public static void RegisterLauncherFactory(this IContainerBuilder builder)
        {
            builder.Register<ILaunherFactory>(container => (launcherController) =>
            {
                var timeManager = container.Resolve<ITimeManager>();
                var projectileFactory = container.Resolve<IProjectileFactory>();
                return new LauncherImpl(launcherController, projectileFactory, timeManager);
            }, Lifetime.Singleton);
        }

        public static void RegsiterLauncher(this IContainerBuilder builder, ILauncherController launcherController)
        {
            builder.RegisterLauncherFactory();
            builder.RegisterInstance<ILauncherController>(launcherController);
            builder.Register<ILauncher, LauncherImpl>(Lifetime.Singleton);
        }
    }
}
