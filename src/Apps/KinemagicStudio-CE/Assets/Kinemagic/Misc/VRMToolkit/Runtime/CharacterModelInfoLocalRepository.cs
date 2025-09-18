using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VRMToolkit
{
    public sealed class CharacterModelInfoLocalRepository : ICharacterModelInfoRepository
    {
        public const string FileExtension = "vrm";

        private readonly List<ICharacterModelInfo> _cachedCharacterModels = new();

        public void Dispose()
        {
            ClearCache();
        }

        public void ClearCache()
        {
            foreach (var modelInfo in _cachedCharacterModels)
            {
                modelInfo.Dispose();
            }
            _cachedCharacterModels.Clear();
        }

        public IReadOnlyList<ICharacterModelInfo> GetAll()
        {
            return _cachedCharacterModels;
        }

        public async UniTask<IReadOnlyList<ICharacterModelInfo>> FetchAllAsync(int limitCount, CancellationToken cancellationToken = default)
        {
            ClearCache();

#if UNITY_EDITOR
            var streamingAssetsDirectory = Application.streamingAssetsPath;

            if (Directory.Exists(streamingAssetsDirectory))
            {
                var storageTypeName = "StreamingAssetsDirectory";
                var files = Directory.GetFiles(streamingAssetsDirectory, $"*.{FileExtension}", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    try
                    {
                        var vrmMetadata = await Vrm10Utils.LoadVrmMetadataAsync(filePath, 256, 256, cancellationToken: cancellationToken);
                        if (vrmMetadata != null)
                        {
                            var resourceKey = Path.GetRelativePath(relativeTo: streamingAssetsDirectory, filePath);
                            _cachedCharacterModels.Add(new VrmCharacterModelInfo(resourceKey, storageTypeName, vrmMetadata.Name, vrmMetadata));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to load VRM metadata from {filePath}: {ex.Message}");
                    }

                    if (_cachedCharacterModels.Count >= limitCount || cancellationToken.IsCancellationRequested)
                    {
                        return _cachedCharacterModels;
                    }
                }
            }
#endif

            var persistentDataDirectory = Application.persistentDataPath;

            if (Directory.Exists(persistentDataDirectory))
            {
                var storageTypeName = "PersistentDataDirectory";
                var files = Directory.GetFiles(persistentDataDirectory, $"*.{FileExtension}", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    try
                    {
                        var vrmMetadata = await Vrm10Utils.LoadVrmMetadataAsync(filePath, 256, 256, cancellationToken: cancellationToken);
                        if (vrmMetadata != null)
                        {
                            var resourceKey = Path.GetRelativePath(relativeTo: persistentDataDirectory, filePath);
                            _cachedCharacterModels.Add(new VrmCharacterModelInfo(resourceKey, storageTypeName, vrmMetadata.Name, vrmMetadata));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to load VRM metadata from {filePath}: {ex.Message}");
                    }

                    if (_cachedCharacterModels.Count >= limitCount || cancellationToken.IsCancellationRequested)
                    {
                        return _cachedCharacterModels;
                    }
                }
            }

            return _cachedCharacterModels;
        }
    }
}
