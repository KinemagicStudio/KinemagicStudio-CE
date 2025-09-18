namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraPropertiesUpdateCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }
        public CameraProperties CameraProperties { get; }
        
        public CameraPropertiesUpdateCommand(CameraId cameraId, CameraProperties cameraProperties)
        {
            CameraId = cameraId;
            CameraProperties = cameraProperties;
        }
    }
}
