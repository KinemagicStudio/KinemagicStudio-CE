using System;
using UnityEngine;

namespace VRMToolkit
{
    public sealed class VrmCharacterModelInfo : ICharacterModelInfo
    {
        public string ResourceKey { get; }
        public string StorageType { get; }
        public string DisplayName { get; }
        public Texture2D Thumbnail { get; }
        public VrmMetadata Metadata { get; }

        public VrmCharacterModelInfo(string resourceKey, string storageType, string displayName, VrmMetadata metadata = null)
        {
            ResourceKey = resourceKey;
            StorageType = storageType;
            DisplayName = displayName;
            Thumbnail = metadata?.Thumbnail;
            Metadata = metadata;
        }

        public void Dispose()
        {
            Metadata?.Dispose();
        }
    }
}
