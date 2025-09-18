namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public class CameraSwitchCommand : ICameraSystemCommand
    {
        public CameraId CameraId { get; private set; }
        public bool IsMainCamera { get; }

        public CameraSwitchCommand(CameraId cameraId, bool isMainCamera)
        {
            CameraId = cameraId;
            IsMainCamera = isMainCamera;
        }
    }
}
