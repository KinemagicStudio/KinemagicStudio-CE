using Klak.Spout;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.VideoStreaming
{
    public sealed class SpoutVideoFrameStreamer : MonoBehaviour, IVideoFrameStreamer
    {
        [SerializeField] SpoutSender _spoutSender;
        [SerializeField] SpoutResources _resources;

        public string Name
        {
            get => _spoutSender.spoutName;
            set => _spoutSender.spoutName = value;
        }

        public bool AlphaSupport
        {
            get => _spoutSender.keepAlpha;
            set => _spoutSender.keepAlpha = value;
        }

        public Texture SourceTexture
        {
            get => _spoutSender.sourceTexture;
            set => _spoutSender.sourceTexture = value;
        }

        public void SetEnable(bool enable)
        {
            if (enable)
            {
                _spoutSender.SetResources(_resources);
                _spoutSender.captureMethod = CaptureMethod.Texture;
                _spoutSender.enabled = true;
            }
            else
            {
                _spoutSender.enabled = false;
            }
        }
    }
}
