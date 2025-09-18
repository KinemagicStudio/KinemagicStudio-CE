using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GLTFast;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class GlbImporter : IEnvironmentSceneProvider
    {
        private readonly IBinaryDataStorage _binaryDataStorage;

        public GlbImporter(IBinaryDataStorage binaryDataStorage)
        {
            _binaryDataStorage = binaryDataStorage;
        }

        public async UniTask<SpatialEnvironmentScene> LoadAsync(string key, BinaryDataStorageType storageType, CancellationToken cancellationToken = default)
        {
            var bytes = await _binaryDataStorage.LoadAsync(key, storageType, cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                // Create a new GameObject to hold the imported model
                var importRoot = new GameObject($"GLTF");

                // Load from the binary data
                var gltf = new GltfImport();
                var loadSuccess = await gltf.Load(bytes, cancellationToken: cancellationToken);
                if (!loadSuccess)
                {
                    Debug.LogError($"Failed to parse GLTF/GLB data from {key}");
                    UnityEngine.Object.Destroy(importRoot);
                    return null;
                }

                // Instantiate the scene in the created game object
                var settings = new InstantiationSettings()
                {
                    Mask = ComponentType.Mesh | ComponentType.Light | ComponentType.Animation
                };
                var instantiator = new GameObjectInstantiator(gltf, importRoot.transform, settings: settings);
                var instantiateSuccess = await gltf.InstantiateMainSceneAsync(instantiator, cancellationToken);
                if (!instantiateSuccess)
                {
                    Debug.LogError($"Failed to instantiate GLTF/GLB scene from {key}");
                    UnityEngine.Object.Destroy(importRoot);
                    return null;
                }

                var sceneInstance = instantiator.SceneInstance;
                if (sceneInstance == null)
                {
                    Debug.LogError($"Scene instance is null");
                    UnityEngine.Object.Destroy(importRoot);
                    return null;
                }

                var scene = new SpatialEnvironmentScene(importRoot);
                if (sceneInstance.Lights != null)
                {
                    foreach (var light in sceneInstance.Lights)
                    {
                        scene.AddLight(light);
                    }
                }
                scene.SetLegacyAnimation(sceneInstance.LegacyAnimation);
                scene.LegacyAnimation?.Play();

                return scene;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Loading GLTF/GLB was canceled.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
    }
}
