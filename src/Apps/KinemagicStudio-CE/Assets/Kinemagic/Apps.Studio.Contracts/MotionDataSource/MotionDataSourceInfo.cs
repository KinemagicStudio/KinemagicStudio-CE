namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{

    public sealed class MotionDataSourceInfo
    {
        public MotionDataSourceKey Key { get; }
        public MotionDataSourceType DataSourceType { get; }
        public DataSourceId DataSourceId { get; }

        public MotionDataSourceInfo(MotionDataSourceKey key, MotionDataSourceType dataSourceType, DataSourceId dataSourceId)
        {
            Key = key;
            DataSourceType = dataSourceType;
            DataSourceId = dataSourceId;
        }
    }
}
