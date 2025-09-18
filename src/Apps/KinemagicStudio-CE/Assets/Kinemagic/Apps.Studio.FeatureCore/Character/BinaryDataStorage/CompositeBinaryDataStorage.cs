using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.Character;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class CompositeBinaryDataStorage : IBinaryDataStorage
    {
        readonly LocalFileBinaryDataStorage _localFileBinaryDataStorage;
#if VROID_SDK_TOOLKIT
        readonly VroidHubBinaryDataStorage _vroidHubBinaryDataStorage;
#endif

        public CompositeBinaryDataStorage(LocalFileBinaryDataStorage localFileBinaryDataStorage
#if VROID_SDK_TOOLKIT
            ,VroidHubBinaryDataStorage vroidHubBinaryDataStorage
#endif
        )
        {
            _localFileBinaryDataStorage = localFileBinaryDataStorage;
#if VROID_SDK_TOOLKIT
            _vroidHubBinaryDataStorage = vroidHubBinaryDataStorage;
#endif
        }

        public async UniTask<byte[]> LoadAsync(string key, BinaryDataStorageType storageType, CancellationToken cancellationToken = default)
        {
            if (storageType == BinaryDataStorageType.LocalFileSystem)
            {
                return await _localFileBinaryDataStorage.LoadAsync(key, storageType, cancellationToken);
            }
#if VROID_SDK_TOOLKIT
            else if (storageType == BinaryDataStorageType.VroidHub)
            {
                return await _vroidHubBinaryDataStorage.LoadAsync(key, storageType, cancellationToken);
            }
#endif
            else
            {
                throw new ArgumentException($"{storageType} is unsupported data storage type", nameof(storageType));
            }
        }
    }
}
