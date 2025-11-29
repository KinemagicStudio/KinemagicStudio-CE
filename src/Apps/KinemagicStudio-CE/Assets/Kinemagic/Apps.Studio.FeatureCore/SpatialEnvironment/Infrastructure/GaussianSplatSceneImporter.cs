using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GaussianSplatting.Runtime;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class GaussianSplatSceneImporter : IEnvironmentSceneProvider
    {
        private readonly GaussianSplatSceneConfig _config;
        private readonly IBinaryDataStorage _binaryDataStorage;
        private readonly GaussianSplatImporter _gaussianSplatImporter;

        public GaussianSplatSceneImporter(GaussianSplatSceneConfig config, IBinaryDataStorage binaryDataStorage)
        {
            _config = config;
            _binaryDataStorage = binaryDataStorage;
            _gaussianSplatImporter = new(GaussianSplatImporter.DataQuality.Medium);
        }

        public async UniTask<SpatialEnvironmentScene> LoadAsync(string key, BinaryDataStorageType storageType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var bytes = await _binaryDataStorage.LoadAsync(key, storageType, cancellationToken);
                if (bytes == null || bytes.Length == 0)
                {
                    return null;
                }

                var name = Path.GetFileNameWithoutExtension(key);
                var splatAsset = _gaussianSplatImporter.Load(bytes, name);
                if (splatAsset == null)
                {
                    Debug.LogError($"Failed to import Gaussian Splat data from {key}");
                    return null;
                }

                // Create a new GameObject to render the splat
                var rootObject = new GameObject($"GaussianSplat_{name}");
                var renderer = rootObject.AddComponent<GaussianSplatRenderer>();

                // Assign the loaded asset
                renderer.m_Asset = splatAsset;

                // Assign shaders if provided
                renderer.m_ShaderSplats = _config.SplatsShader;
                renderer.m_ShaderComposite = _config.CompositeShader;
                renderer.m_ShaderDebugPoints = _config.DebugPointsShader;
                renderer.m_ShaderDebugBoxes = _config.DebugBoxesShader;
                renderer.m_CSSplatUtilities = _config.SplatUtilitiesComputeShader;

                // Apply display settings
                renderer.m_SplatScale = 1.0f;
                renderer.m_OpacityScale = 1.0f;
                renderer.m_SHOrder = 3;

                // Force enable to trigger resource creation
                renderer.enabled = false;
                renderer.enabled = true;

                return new SpatialEnvironmentScene(rootObject);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Loading Gaussian Splat was canceled.");
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
