namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public readonly struct MotionDataSourceStatus
    {
        public readonly DataSourceId DataSourceId;
        public readonly ProcessingStatus ProcessingStatus;

        public MotionDataSourceStatus(DataSourceId dataSourceId, ProcessingStatus processingStatus)
        {
            DataSourceId = dataSourceId;
            ProcessingStatus = processingStatus;
        }
    }

    public enum ProcessingStatus
    {
        NotStarted,
        InProgress,
        Stalled,
        Completed,
    }
}
