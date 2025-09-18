using EngineLooper.VContainer;
using Kinemagic.Apps.Studio.FeatureCore.Character;
using Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Kinemagic.Apps.Studio.Lifecycle
{
    public sealed class StageLifetimeScope : LifetimeScope
    {
        [Header("Character System")]
        [SerializeField] SpatialCoordinateProvider _spatialCoordinateProvider;

        protected override void Configure(IContainerBuilder builder)
        {
            ConfigureCharacterSystem(builder);
            ConfigureEnvironmentSystem(builder);
            Debug.Log($"<color=cyan>[{nameof(StageLifetimeScope)}] Configured</color>");
        }

        private void ConfigureCharacterSystem(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<CharacterSystem>();

            builder.Register<CharacterSystemConfig>(Lifetime.Singleton)
                .WithParameter("persistentDataPath", Application.persistentDataPath)
                .WithParameter("streamingAssetsPath", Application.streamingAssetsPath);

            builder.Register<CharacterInstanceRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<CharacterPoseHandlerRegistry>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<VRMToolkit.CharacterModelInfoLocalRepository>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.Register<VrmCharacterProvider>(Lifetime.Singleton);
            builder.Register<CompositeBinaryDataStorage>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FeatureCore.Character.LocalFileBinaryDataStorage>(Lifetime.Singleton).AsSelf();

            builder.RegisterInstance(_spatialCoordinateProvider);
        }

        private void ConfigureEnvironmentSystem(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<EnvironmentSystem>();

            builder.Register<EnvironmentModelInfoLocalRepository>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.Register<GlbImporter>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FeatureCore.SpatialEnvironment.LocalFileBinaryDataStorage>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
