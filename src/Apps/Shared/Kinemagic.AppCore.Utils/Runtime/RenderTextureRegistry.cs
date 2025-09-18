using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kinemagic.AppCore.Utils
{
    public sealed class RenderTextureRegistry : IDisposable
    {
        private readonly Dictionary<string, RenderTexture> _textures = new();

        public void Dispose()
        {
            foreach (var texture in _textures.Values)
            {
                UnityEngine.Object.Destroy(texture);
            }
            _textures.Clear();
        }

        public RenderTexture GetOrCreate(string key, int width, int height)
        {
            if (_textures.TryGetValue(key, out var texture))
            {
                return texture;
            }

            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            texture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
            texture.name = key;

            _textures[key] = texture;
            return texture;
        }

        public bool TryGet(string key, out RenderTexture texture)
        {
            return _textures.TryGetValue(key, out texture);
        }

        public void Delete(string key)
        {
            if (_textures.Remove(key, out var texture))
            {
                texture.Release();
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
