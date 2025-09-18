using System;
using UniGLTF.Extensions.VRMC_vrm;
using UniVRM10.Migration;
using UnityEngine;

namespace VRMToolkit
{
    public sealed class VrmMetadata : IDisposable
    {
        public Texture2D Thumbnail { get; private set; }
        public Meta Vrm10Meta { get; private set; }
        public Vrm0Meta Vrm0Meta { get; private set; }

        public string Name => Vrm10Meta?.Name ?? Vrm0Meta?.title ?? "Unknown";

        public VrmMetadata(Texture2D thumbnail, Meta vrm10Meta, Vrm0Meta vrm0Meta)
        {
            Thumbnail = thumbnail;
            Vrm10Meta = vrm10Meta;
            Vrm0Meta = vrm0Meta;
        }

        public void Dispose()
        {
            if (Thumbnail != null)
            {
                UnityEngine.Object.Destroy(Thumbnail);
                Thumbnail = null;
            }
            Vrm10Meta = null;
            Vrm0Meta = null;
        }
    }
}