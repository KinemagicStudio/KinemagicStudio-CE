namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class BodyTrackingDataUpdatedSignal : IMotionDataSignal
    {
        public int DataSourceId { get; private set; } = -1;
        public BodyTrackingFrame BodyTrackingFrame { get; private set; }

        public void SetData(int dataSourceId, BodyTrackingFrame bodyTrackingFrame)
        {
            DataSourceId = dataSourceId;
            BodyTrackingFrame = bodyTrackingFrame;
        }
    }
}