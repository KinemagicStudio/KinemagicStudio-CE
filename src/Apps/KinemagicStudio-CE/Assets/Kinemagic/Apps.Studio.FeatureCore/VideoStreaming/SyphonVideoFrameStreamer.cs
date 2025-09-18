using Klak.Syphon;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.VideoStreaming
{
    public sealed class SyphonVideoFrameStreamer : MonoBehaviour, IVideoFrameStreamer
    {
        [SerializeField] SyphonServer _syphonServer;
        [SerializeField] SyphonResources _resources;

        public string Name
        {
            get => _syphonServer.ServerName;
            set => _syphonServer.ServerName = value;
        }

        public bool AlphaSupport
        {
            get => _syphonServer.KeepAlpha;
            set => _syphonServer.KeepAlpha = value;
        }

        public Texture SourceTexture
        {
            get => _syphonServer.SourceTexture;
            set => _syphonServer.SourceTexture = value;
        }

        public void SetEnable(bool enable)
        {
            if (enable)
            {
                _syphonServer.Resources = _resources;
                _syphonServer.CaptureMethod = CaptureMethod.Texture;
                _syphonServer.enabled = true;
            }
            else
            {   
                _syphonServer.enabled = false;
            }
        }
    }
}
