namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public class PostProcessingParametersNotifyCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }

        public PostProcessingParametersNotifyCommand(CameraId cameraId)
        {
            CameraId = cameraId;
        }
    }
}
