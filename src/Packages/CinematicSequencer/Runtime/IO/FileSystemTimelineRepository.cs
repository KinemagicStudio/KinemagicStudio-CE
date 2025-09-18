using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CinematicSequencer.Serialization;
using Cysharp.Threading.Tasks;

namespace CinematicSequencer.IO
{
    public sealed class FileSystemTimelineRepository : ITimelineRepository
    {
        private readonly ITimelineSerializer _serializer;
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

        public FileSystemTimelineRepository(ITimelineSerializer serializer, string fileExtension)
        {
            _defaultDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CinematicSequencer");
            _serializer = serializer;
            _fileExtension = fileExtension;
        }

        public FileSystemTimelineRepository(ITimelineSerializer serializer, string fileExtension, string directoryPath)
        {
            _defaultDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CinematicSequencer");
            _serializer = serializer;
            _fileExtension = fileExtension;
            DirectoryPath = directoryPath;
        }
        
        public async UniTask<List<CinematicSequenceDataInfo>> GetSequenceDataInfoListAsync(CancellationToken cancellationToken)
        {
            var sequenceDataInfoList = new List<CinematicSequenceDataInfo>();

            if (!Directory.Exists(DirectoryPath))
            {
                return sequenceDataInfoList;
            }

            var files = Directory.GetFiles(DirectoryPath, $"*.{_fileExtension}", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var data = await File.ReadAllBytesAsync(file, cancellationToken);
                if (_serializer.TryGetFormatVersion(data, out var formatVersion)
                && _serializer.TryGetSequenceDataInfo(data, out var info))
                {
                    sequenceDataInfoList.Add(info);
                }
            }

            return sequenceDataInfoList;
        }

        public async UniTask SaveAsync(string key, Timeline timeline, CancellationToken cancellationToken)
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

            var data = _serializer.Serialize(timeline);
            await File.WriteAllBytesAsync(filePath, data, cancellationToken);
        }

        public async UniTask<Timeline> LoadAsync(string key, CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(DirectoryPath, $"*{key}*.{_fileExtension}", SearchOption.AllDirectories);
            var filePath = files.Length > 0 ? files[0] : "";

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Sequence data file not found: {filePath}");
            }

            var data = await File.ReadAllBytesAsync(filePath, cancellationToken);

            if (_serializer.TryGetSequenceDataInfo(data, out var info))
            {
                return _serializer.Deserialize<Timeline>(data);
            }
            else
            {
                throw new InvalidDataException($"Failed to load sequence data. Key: {key}");
            }
        }
    }
}
