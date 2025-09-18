using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.VideoStreaming
{
    public interface IVideoFrameStreamer
    {
        string Name { get; set; }
        bool AlphaSupport { get; set; }
        Texture SourceTexture { get; set; }
        void SetEnable(bool enable);
    }
}
