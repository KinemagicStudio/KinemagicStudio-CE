using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class CompositeEnvironmentSceneProvider : IEnvironmentSceneProvider
    {
        private readonly GlbImporter _glbImporter;
        private readonly GaussianSplatSceneImporter _gaussianSplatImporter;

        public CompositeEnvironmentSceneProvider(
            GlbImporter glbImporter,
            GaussianSplatSceneImporter gaussianSplatImporter)
        {
            _glbImporter = glbImporter ?? throw new ArgumentNullException(nameof(glbImporter));
            _gaussianSplatImporter = gaussianSplatImporter ?? throw new ArgumentNullException(nameof(gaussianSplatImporter));
        }

        public async UniTask<SpatialEnvironmentScene> LoadAsync(string key, BinaryDataStorageType storageType,
            CancellationToken cancellationToken = default)
        {
            var provider = SelectProvider(key);
            if (provider == null)
            {
                Debug.LogError($"Unsupported file format: {key}");
                return null;
            }

            return await provider.LoadAsync(key, storageType, cancellationToken);
        }

        private IEnvironmentSceneProvider SelectProvider(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".glb" => _glbImporter,
                ".spz" => _gaussianSplatImporter,
                _ => null
            };
        }
    }
}
