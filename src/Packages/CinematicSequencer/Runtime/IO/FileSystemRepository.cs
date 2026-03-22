using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CinematicSequencer.Serialization;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    /// <summary>
    /// ファイルシステムベースのリポジトリ。
    /// 現行のFileSystemTimelineRepositoryとFileSystemClipDataRepositoryを統合。
    /// </summary>
    public sealed class FileSystemRepository : ISequenceRepository, IClipAssetRepository
    {
        private readonly ISequenceSerializer _serializer;
        private readonly string _basePath;
        private readonly string _fileExtension;

        public string SequenceDirectory => Path.Combine(_basePath, "Sequences");
        public string ClipAssetDirectory => Path.Combine(_basePath, "ClipAssets");

        public FileSystemRepository(ISequenceSerializer serializer, string basePath, string fileExtension = "json")
        {
            _serializer = serializer;
            _basePath = basePath;
            _fileExtension = fileExtension;
        }

        // --- ISequenceRepository ---

        public async UniTask<Sequence> LoadSequenceAsync(Guid id, CancellationToken ct)
        {
            var filePath = GetSequenceFilePath(id);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Sequence file not found: {filePath}");
            }
            var data = await File.ReadAllBytesAsync(filePath, ct);
            return _serializer.DeserializeSequence(data);
        }

        public async UniTask SaveSequenceAsync(Sequence sequence, CancellationToken ct)
        {
            EnsureDirectoryExists(SequenceDirectory);
            var filePath = GetSequenceFilePath(sequence.Id);
            var data = _serializer.SerializeSequence(sequence);
            await File.WriteAllBytesAsync(filePath, data, ct);
        }

        public async UniTask<List<SequenceInfo>> GetSequenceListAsync(CancellationToken ct)
        {
            var result = new List<SequenceInfo>();
            if (!Directory.Exists(SequenceDirectory)) return result;

            var files = Directory.GetFiles(SequenceDirectory, $"*.{_fileExtension}");
            foreach (var file in files)
            {
                var data = await File.ReadAllBytesAsync(file, ct);
                try
                {
                    var sequence = _serializer.DeserializeSequence(data);
                    result.Add(new SequenceInfo(sequence.Id, sequence.Name, sequence.Tracks.Count));
                }
                catch (Exception)
                {
                    // Skip files that fail to parse
                }
            }
            return result;
        }

        // --- IClipAssetRepository ---

        public async UniTask<IClipAsset> LoadClipAssetAsync(Guid id, CancellationToken ct)
        {
            var filePath = GetClipAssetFilePath(id);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"ClipAsset file not found: {filePath}");
            }
            var data = await File.ReadAllBytesAsync(filePath, ct);
            return _serializer.DeserializeClipAsset(data);
        }

        public async UniTask SaveClipAssetAsync(IClipAsset asset, CancellationToken ct)
        {
            EnsureDirectoryExists(ClipAssetDirectory);
            var filePath = GetClipAssetFilePath(asset.Id);
            var data = _serializer.SerializeClipAsset(asset);
            await File.WriteAllBytesAsync(filePath, data, ct);
        }

        public async UniTask<List<ClipAssetInfo>> GetClipAssetListAsync(CancellationToken ct)
        {
            var result = new List<ClipAssetInfo>();
            if (!Directory.Exists(ClipAssetDirectory)) return result;

            var files = Directory.GetFiles(ClipAssetDirectory, $"*.{_fileExtension}");
            foreach (var file in files)
            {
                var data = await File.ReadAllBytesAsync(file, ct);
                try
                {
                    var clipAsset = _serializer.DeserializeClipAsset(data);
                    result.Add(new ClipAssetInfo(clipAsset.Id, clipAsset.Name, clipAsset.Type));
                }
                catch (Exception)
                {
                    // Skip files that fail to parse
                }
            }
            return result;
        }

        // --- Private helpers ---

        private string GetSequenceFilePath(Guid id)
        {
            return Path.Combine(SequenceDirectory, $"{id}.{_fileExtension}");
        }

        private string GetClipAssetFilePath(Guid id)
        {
            return Path.Combine(ClipAssetDirectory, $"{id}.{_fileExtension}");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
