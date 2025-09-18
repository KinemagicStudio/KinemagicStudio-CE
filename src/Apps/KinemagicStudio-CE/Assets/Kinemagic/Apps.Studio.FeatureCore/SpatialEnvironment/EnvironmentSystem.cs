using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using EngineLooper;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using MessagePipe;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class EnvironmentSystem : IDisposable, IInitializable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly LightManager _lightManager = new();
        private readonly IEnvironmentSceneProvider _environmentSceneProvider;
        private readonly ISubscriber<IEnvironmentSystemCommand> _commandSubscriber;

        private IDisposable _disposable;
        private SpatialEnvironmentScene _currentScene;

        public EnvironmentSystem(
            IEnvironmentSceneProvider environmentSceneProvider,
            ISubscriber<IEnvironmentSystemCommand> commandSubscriber)
        {
            _environmentSceneProvider = environmentSceneProvider;
            _commandSubscriber = commandSubscriber;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _disposable?.Dispose();

            _currentScene?.Dispose();
            _currentScene = null;

            _lightManager.Clear();
        }

        public void Initialize()
        {
            InitializeAsync().Forget();
        }

        public async UniTask InitializeAsync()
        {
            await StreamingAssetsSynchronizer.SynchronizeFilesAsync(Constants.PersistentDataDirectoryName, "*.glb", _cancellationTokenSource.Token);

            _currentScene = SpatialEnvironmentScene.CreateDefault();

            var d = DisposableBag.CreateBuilder();
            _commandSubscriber.Subscribe(HandleEnvironmentSystemCommand).AddTo(d);
            _disposable = d.Build();
        }

        private void HandleEnvironmentSystemCommand(IEnvironmentSystemCommand command)
        {
            switch (command)
            {
                case SceneInstanceCreateCommand sceneInstanceCreateCommand:
                    CreateSceneInstanceAsync(sceneInstanceCreateCommand).Forget();
                    break;
            }
        }

        private async UniTask CreateSceneInstanceAsync(SceneInstanceCreateCommand command)
        {
            var binaryDataStorageType = command.StorageType switch
            {
                "PersistentDataDirectory" => BinaryDataStorageType.LocalFileSystem,
                "StreamingAssetsDirectory" => BinaryDataStorageType.LocalFileSystem,
                _ => throw new NotSupportedException($"Unsupported storage type: {command.StorageType}")
            };
            
            var resourceKey = command.ResourceKey;
            if (command.StorageType == "PersistentDataDirectory")
            {
                resourceKey = Path.Combine(Application.persistentDataPath, command.ResourceKey);
            }
            else if (command.StorageType == "StreamingAssetsDirectory")
            {
                resourceKey = Path.Combine(Application.streamingAssetsPath, command.ResourceKey);
            }

            if (_currentScene != null)
            {
                _currentScene.Dispose();
                _currentScene = null;
            }

            var scene = await _environmentSceneProvider.LoadAsync(resourceKey, binaryDataStorageType, _cancellationTokenSource.Token);
            if (scene == null)
            {
                throw new InvalidOperationException($"Failed to load scene from resource: {resourceKey}");
            }

            _currentScene = scene;

            for (var i = 0; i < scene.Lights.Count; i++)
            {
                _lightManager.Add(i, scene.Lights[i]);
            }
        }
    }
}
