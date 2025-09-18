using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CinematicSequencer.Serialization;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    public sealed class FileSystemClipDataRepository : IClipDataRepository
    {
        private readonly IClipDataSerializer _serializer;
        private readonly string _fileExtension = "json";
        private readonly string _defaultDirectoryPath;

        private string _directoryPath;

        public string DirectoryPath
        {
            get => _directoryPath ?? _defaultDirectoryPath;
            set
            {
                var parent = Directory.GetParent(value);
                if (parent != null && parent.Exists)
                {
                    _directoryPath = value;
                }
                else
                {
                    throw new DirectoryNotFoundException($"The directory does not exist: {parent}");
                }
            }
        }

        public FileSystemClipDataRepository(IClipDataSerializer serializer, string fileExtension)
        {
            _defaultDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CinematicSequencer");
            _serializer = serializer;
            _fileExtension = fileExtension;
        }

        public FileSystemClipDataRepository(IClipDataSerializer serializer, string fileExtension, string directoryPath)
        {
            _defaultDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CinematicSequencer");
            _serializer = serializer;
            _fileExtension = fileExtension;
            DirectoryPath = directoryPath;
        }

        public async UniTask<List<ClipDataInfo>> GetClipDataInfoListAsync(CancellationToken cancellationToken)
        {
            var clipDataInfoList = new List<ClipDataInfo>();

            if (!Directory.Exists(DirectoryPath))
            {
                return clipDataInfoList;
            }

            var files = Directory.GetFiles(DirectoryPath, $"*.{_fileExtension}", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var data = await File.ReadAllBytesAsync(file, cancellationToken);
                if (_serializer.TryGetFormatVersion(data, out var formatVersion)
                && _serializer.TryGetClipDataInfo(data, out var info))
                {
                    clipDataInfoList.Add(info);
                }
            }

            return clipDataInfoList;
        }

        public async UniTask<IClipData> LoadAsync(string key, CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(DirectoryPath, $"*{key}*.{_fileExtension}", SearchOption.AllDirectories);
            var filePath = files.Length > 0 ? files[0] : "";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Clip data file not found: {filePath}");
            }

            var data = await File.ReadAllBytesAsync(filePath, cancellationToken);

            if (_serializer.TryGetClipDataInfo(data, out var info))
            {
                if (info.Type == DataType.CameraPose || info.Type == DataType.LightPose)
                {
                    return _serializer.Deserialize<PoseAnimation>(data);
                }
                else if (info.Type == DataType.LightProperties)
                {
                    return _serializer.Deserialize<LightPropertiesAnimation>(data);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported clip type: {info.Type}");
                }
            }
            else
            {
                throw new InvalidDataException($"Failed to load clip data for key: {key}");
            }
        }

        public async UniTask SaveAsync(string key, IClipData clipData, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }

            var filePath = Path.Combine(DirectoryPath, $"{key}.{_fileExtension}");
            var subdirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(subdirectory) && !Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var data = _serializer.Serialize(clipData);
            await File.WriteAllBytesAsync(filePath, data, cancellationToken);
        }
    }
}
