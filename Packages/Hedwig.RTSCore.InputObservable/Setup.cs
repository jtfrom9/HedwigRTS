#nullable enable

using System;
using VContainer;

namespace Hedwig.RTSCore.InputObservable
{
    public static class LauncherDIExtension
    {
        public static void Setup(this IContainerBuilder builder, InputObservableMouseHandler? handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler is null");
            }
            builder.RegisterInstance<InputObservableMouseHandler>(handler)
                .AsImplementedInterfaces();
        }
    }
}