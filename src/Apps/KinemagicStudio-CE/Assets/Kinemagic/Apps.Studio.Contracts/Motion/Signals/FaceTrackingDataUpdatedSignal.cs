namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class FaceTrackingDataUpdatedSignal : IMotionDataSignal
    {
        public int DataSourceId { get; private set; } = -1;
        public FaceTrackingFrame FaceTrackingFrame { get; private set; }

        public void SetData(int dataSourceId, FaceTrackingFrame faceTrackingFrame)
        {
            DataSourceId = dataSourceId;
            FaceTrackingFrame = faceTrackingFrame;
        }
    }
}
