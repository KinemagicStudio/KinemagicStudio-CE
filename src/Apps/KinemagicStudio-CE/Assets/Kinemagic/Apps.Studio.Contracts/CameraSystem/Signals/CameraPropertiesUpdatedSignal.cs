namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public sealed class CameraPropertiesUpdatedSignal : ICameraSystemSignal
    {
        public CameraId CameraId { get; }
        public CameraProperties CameraProperties { get; }

        public CameraPropertiesUpdatedSignal(CameraId cameraId, CameraProperties cameraProperties)
        {
            CameraId = cameraId;
            CameraProperties = cameraProperties;
        }
    }
}
