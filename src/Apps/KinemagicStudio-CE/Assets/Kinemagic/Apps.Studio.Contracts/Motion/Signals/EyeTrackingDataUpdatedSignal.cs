namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class EyeTrackingDataUpdatedSignal : IMotionDataSignal
    {
        public int DataSourceId { get; private set; } = -1;
        public EyeTrackingFrame EyeTrackingFrame { get; private set; }

        public void SetData(int dataSourceId, EyeTrackingFrame eyeTrackingFrame)
        {
            DataSourceId = dataSourceId;
            EyeTrackingFrame = eyeTrackingFrame;
        }
    }
}
