using System;
using UnityEngine;

namespace VRMToolkit
{
    public interface ICharacterModelInfo : IDisposable
    {
        string ResourceKey { get; }
        string StorageType { get; }
        string DisplayName { get; }
        Texture2D Thumbnail { get; }
    }
}
