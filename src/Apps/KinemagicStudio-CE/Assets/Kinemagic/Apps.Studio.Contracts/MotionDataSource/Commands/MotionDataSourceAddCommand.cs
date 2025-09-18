namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public sealed class MotionDataSourceAddCommand : IMotionDataSourceCommand
    {
        public MotionDataSourceKey Key { get; }
        public MotionDataSourceType DataSourceType { get; }

        public MotionDataSourceAddCommand(MotionDataSourceKey key, MotionDataSourceType dataSourceType)
        {
            Key = key;
            DataSourceType = dataSourceType;
        }
    }
}
