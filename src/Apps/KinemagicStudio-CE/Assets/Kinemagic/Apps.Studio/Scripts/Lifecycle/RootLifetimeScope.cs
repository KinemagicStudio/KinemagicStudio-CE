using System.IO;
using CinematicSequencer;
using CinematicSequencer.IO;
using CinematicSequencer.Serialization;
using EngineLooper.VContainer;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.CameraSystem;
using Kinemagic.Apps.Studio.Contracts.Character;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using Kinemagic.Apps.Studio.FeatureCore.CinematicSequencer;
using Kinemagic.Apps.Studio.FeatureCore.MotionDataSource;
using Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure;
using Kinemagic.Apps.Studio.UI;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.Lifecycle
{
    public sealed class RootLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            ConfigureMessagePipe(builder);
            ConfigureMotionDataSourceSystem(builder);
            ConfigureCinematicSequencer(builder);

            builder.Register<UIViewContext>(Lifetime.Singleton);
            builder.Register<RenderTextureRegistry>(Lifetime.Singleton).AsSelf();

            builder.RegisterBuildCallback(container =>
            {
                if (GlobalContextProvider.UIViewContext != null)
                {
                    Debug.Log($"<color=orange>[{nameof(RootLifetimeScope)}] GlobalContextProvider.UIViewContext is already set</color>");
                }
                else
                {
                    GlobalContextProvider.UIViewContext = container.Resolve<UIViewContext>();
                }

                if (GlobalRenderTextureRegistry.IsInitialized)
                {
                    Debug.Log($"<color=orange>[{nameof(RootLifetimeScope)}] GlobalRenderTextureRegistry is already initialized</color>");
                }
                else
                {
                    GlobalRenderTextureRegistry.Set(container.Resolve<RenderTextureRegistry>());
                }
            });

            Debug.Log($"<color=cyan>[{nameof(RootLifetimeScope)}] Configured</color>");
        }

        private void ConfigureMotionDataSourceSystem(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<MotionDataSourceSystem>(Lifetime.Singleton);

            builder.Register<MotionDataSourceRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MotionDataSourceMonitor>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            builder.Register<MotionDataSourceManager>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SequentialIdPool>(Lifetime.Singleton).WithParameter("firstId", 1).AsSelf();
            builder.Register<VMCProtocolDataSourceManager>(Lifetime.Singleton).AsSelf();
#if FACIAL_CAPTURE_SYNC
            builder.Register<FacialMocapDataSourceManager>(Lifetime.Singleton).AsSelf();
#endif
#if MOCOPI_RECEIVER_PLUGIN
            builder.Register<MocopiDataSourceManager>(Lifetime.Singleton).AsSelf();
#endif
        }

        private void ConfigureCinematicSequencer(IContainerBuilder builder)
        {
            var dataPath = $"{UnityEngine.Application.persistentDataPath}/CinematicSequencer";
            var directoryInfo = Directory.CreateDirectory(dataPath);
            var sequenceDataDirectoryPath = $"{directoryInfo.FullName}/SequenceData";
            var clipDataDirectoryPath = $"{directoryInfo.FullName}/ClipData";

            var timelineRepository = new FileSystemTimelineRepository(new JsonTimelineSerializer(), "json", sequenceDataDirectoryPath);
            var clipDataRepository = new FileSystemClipDataRepository(new JsonClipDataSerializer(), "json", clipDataDirectoryPath);

            var sequencePlayer = CinematicSequenceSystem.SequencePlayer;
            var animationEditor = new KeyframeAnimationEditor(clipDataRepository, sequencePlayer);

            builder.RegisterInstance(timelineRepository).AsImplementedInterfaces();
            builder.RegisterInstance(clipDataRepository).AsImplementedInterfaces();
            builder.RegisterInstance(sequencePlayer).AsImplementedInterfaces();
            builder.RegisterInstance(animationEditor).AsImplementedInterfaces();

            builder.RegisterEngineLooperEntryPoint<CinematicSequenceSystemAdapter>();
        }

        private void ConfigureMessagePipe(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe(options =>
            {
                options.EnableCaptureStackTrace = true;
            });

            builder.RegisterMessageBroker<ICameraSystemCommand>(options);
            builder.RegisterMessageBroker<ICameraSystemSignal>(options);

            builder.RegisterMessageBroker<ICharacterCommand>(options);

            builder.RegisterMessageBroker<IMotionDataSourceCommand>(options);
            builder.RegisterMessageBroker<IMotionDataSignal>(options);

            builder.RegisterBuildCallback(container =>
            {
                if (GlobalMessagePipe.IsInitialized)
                {
                    Debug.Log($"<color=orange>[{nameof(RootLifetimeScope)}] GlobalMessagePipe is already initialized</color>");
                }
                else
                {
                    GlobalMessagePipe.SetProvider(container.AsServiceProvider());
                }
            });
        }
    }
}
