using System.Collections.Generic;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class PostProcessingUpdatedSignal : ICameraSystemSignal
    {
        public CameraId CameraId { get; }
        public IPostProcessingParameters Parameters { get; }

        public PostProcessingUpdatedSignal(CameraId cameraId, IPostProcessingParameters parameters)
        {
            CameraId = cameraId;
            Parameters = parameters;
        }
    }
}
