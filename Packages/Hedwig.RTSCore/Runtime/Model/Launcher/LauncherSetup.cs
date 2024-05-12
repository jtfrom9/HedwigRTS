#nullable enable

using VContainer;

using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    public static class LauncherDIExtension
    {
        public static void SetupLauncher(this IContainerBuilder builder)
        {
            builder.Register<LauncherImpl>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterInstance<ILauncherController>(ControllerBase.Find<ILauncherController>());
        }
    }
}
