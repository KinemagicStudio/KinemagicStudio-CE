#if ENABLE_MONO || ENABLE_IL2CPP
#define UNITY_ENGINE
#endif

using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.Character;

#if UNITY_ENGINE
using UnityEngine.Networking;
#endif

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class LocalFileBinaryDataStorage : IBinaryDataStorage
    {
        public async UniTask<byte[]> LoadAsync(string key, BinaryDataStorageType storageType, CancellationToken cancellationToken = default)
        {
            if (storageType != BinaryDataStorageType.LocalFileSystem)
            {
                throw new ArgumentException($"{storageType} is unsupported data storage type", nameof(storageType));
            }

#if UNITY_ENGINE
            // NOTE: Enable to load streaming asset files on Android.
            if (Uri.IsWellFormedUriString(key, UriKind.Absolute))
            {
                var webRequest = UnityWebRequest.Get(key);
                await webRequest.SendWebRequest();
                return webRequest.downloadHandler.data;
            }
            else
#endif
            {
                return await File.ReadAllBytesAsync(key, cancellationToken);
            }
        }
    }
}
