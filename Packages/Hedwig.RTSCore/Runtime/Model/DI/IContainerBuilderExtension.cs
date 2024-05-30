#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;
using Hedwig.RTSCore.Impl;

namespace Hedwig.RTSCore.Model
{
    public static class IContainerBuilderExtension
    {
        class DummyTimeManager : ITimeManager
        {
            public IReadOnlyReactiveProperty<bool> Paused { get; private set; }

            public DummyTimeManager()
            {
                Paused = new ReactiveProperty<bool>(false);
            }
        }

        public static void RegisterTimeManager(this IContainerBuilder builder, ITimeManager? timeManager)
        {
            if (timeManager != null)
            {
                builder.RegisterInstance(timeManager);
            }
            else
            {
                builder.Register<ITimeManager, DummyTimeManager>(Lifetime.Singleton);
            }
        }

        public static void Setup(this IContainerBuilder builder,
            ITimeManager? timeManager,
            ILauncherController? launcherController,
            UnitManagerObject? unitManager,
            GlobalVisualizersObject? visualizers,
            IEnumerable<UnitObject>? units = null,
            UnitObject? unit = null)
        {
            builder.RegisterTimeManager(timeManager);
            if (launcherController != null)
            {
                builder.RegsiterLauncher(launcherController);
            }
            else
            {
                builder.RegisterLauncherFactory();
            }
            if (unitManager != null)
            {
                builder.RegisterUnitManager(unitManager);
            }
            if (visualizers != null)
            {
                builder.RegisterVisualizer(visualizers);
            }
            if (unit != null)
            {
                builder.RegisterUnit(unit);
            }
            if (units != null)
            {
                builder.RegisterUnits(units);
            }
        }
    }
}
