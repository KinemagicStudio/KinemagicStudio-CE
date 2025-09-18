namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraPropertiesNotifyCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; }

        public CameraPropertiesNotifyCommand(CameraId cameraId)
        {
            CameraId = cameraId;
        }
    }
}