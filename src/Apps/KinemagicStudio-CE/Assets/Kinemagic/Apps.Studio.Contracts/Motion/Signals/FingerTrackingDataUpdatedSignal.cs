namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class FingerTrackingDataUpdatedSignal : IMotionDataSignal
    {
        public int DataSourceId { get; private set; } = -1;
        public FingerTrackingFrame FingerTrackingFrame { get; private set; }

        public void SetData(int dataSourceId, FingerTrackingFrame fingerTrackingFrame)
        {
            DataSourceId = dataSourceId;
            FingerTrackingFrame = fingerTrackingFrame;
        }
    }
}