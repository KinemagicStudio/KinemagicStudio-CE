namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public class PostProcessingUpdateCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }
        public IPostProcessingParameters Parameters { get; }

        public PostProcessingUpdateCommand(CameraId cameraId, IPostProcessingParameters parameters)
        {
            CameraId = cameraId;
            Parameters = parameters;
        }
    }
}
