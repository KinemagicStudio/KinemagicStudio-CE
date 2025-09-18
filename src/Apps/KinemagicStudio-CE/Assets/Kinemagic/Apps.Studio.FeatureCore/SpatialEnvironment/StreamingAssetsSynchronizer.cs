using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    internal static class StreamingAssetsSynchronizer
    {
        internal static async UniTask SynchronizeFilesAsync(string folderName, string filePattern, CancellationToken cancellationToken)
        {
            var sourceDir = Path.Combine(Application.streamingAssetsPath, folderName);
            var targetDir = Path.Combine(Application.persistentDataPath, folderName);

            if (!Directory.Exists(sourceDir)) return;

            try
            {
                Directory.CreateDirectory(targetDir);
                
                var sourceFiles = Directory.GetFiles(sourceDir, filePattern, SearchOption.AllDirectories);

                foreach (var sourceFile in sourceFiles)
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                        var targetFile = Path.Combine(targetDir, relativePath);

                        if (await ShouldCopyFileAsync(sourceFile, targetFile))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                            await CopyFileAsync(sourceFile, targetFile, cancellationToken);
                            Debug.Log($"Copied: {relativePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to copy file {sourceFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to ensure environment files: {ex.Message}");
            }
        }

        private static async UniTask<bool> ShouldCopyFileAsync(string sourceFile, string targetFile)
        {
            if (!File.Exists(targetFile)) return true;

            var sourceInfo = new FileInfo(sourceFile);
            var targetInfo = new FileInfo(targetFile);

            // Copy required if file sizes are different
            if (sourceInfo.Length != targetInfo.Length) return true;

            // Compare file contents using hash if sizes are the same
            var sourceHash = await ComputeFileHashAsync(sourceFile);
            var targetHash = await ComputeFileHashAsync(targetFile);

            return sourceHash != targetHash;
        }

        private static async UniTask<string> ComputeFileHashAsync(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            var hashBytes = sha256.ComputeHash(buffer);
            return Convert.ToBase64String(hashBytes);
        }

        private static async UniTask CopyFileAsync(string sourceFile, string targetFile, CancellationToken cancellationToken)
        {
            using var source = File.OpenRead(sourceFile);
            using var target = File.Create(targetFile);
            await source.CopyToAsync(target, cancellationToken);
        }
    }
}