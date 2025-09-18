using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class EnvironmentModelInfoLocalRepository : IEnvironmentModelInfoRepository, IDisposable
    {
        public const string FileExtension = "glb";

        private readonly List<EnvironmentModelInfo> _cachedModels = new();
        private readonly string[] _directories = {
#if UNITY_EDITOR
            Application.streamingAssetsPath,
#endif
            Application.persistentDataPath
        };

        public void Dispose()
        {
            ClearCache();
        }

        public void ClearCache()
        {
            _cachedModels.Clear();
        }

        public IReadOnlyList<EnvironmentModelInfo> GetAll()
        {
            return _cachedModels;
        }

        public async UniTask<IReadOnlyList<EnvironmentModelInfo>> FetchAllAsync(CancellationToken cancellationToken = default)
        {
            ClearCache();

            foreach (var directory in _directories)
            {
                if (Directory.Exists(directory))
                {
                    await ScanDirectoryAsync(directory, cancellationToken);
                }
            }

            return _cachedModels;
        }

        private async UniTask ScanDirectoryAsync(string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, $"*.{FileExtension}", SearchOption.AllDirectories);
                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var modelInfo = CreateModelInfoAsync(directoryPath, filePath);
                    if (modelInfo != null)
                    {
                        _cachedModels.Add(modelInfo);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to scan directory {directoryPath}: {ex}");
            }
        }

        private EnvironmentModelInfo CreateModelInfoAsync(string directoryPath, string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) return null;

                var storageType = GetStorageTypeFromPath(directoryPath);
                var resourceKey = Path.GetRelativePath(directoryPath, filePath);
                var displayName = Path.GetFileNameWithoutExtension(filePath);

                return new EnvironmentModelInfo(resourceKey, storageType, displayName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create model info for {filePath}: {ex}");
                return null;
            }
        }

        private string GetStorageTypeFromPath(string path)
        {
            if (path.StartsWith(Application.streamingAssetsPath))
            {
                return "StreamingAssetsDirectory";
            }
            else if (path.StartsWith(Application.persistentDataPath))
            {
                return "PersistentDataDirectory";
            }
            return "LocalFileSystem";
        }
    }
}