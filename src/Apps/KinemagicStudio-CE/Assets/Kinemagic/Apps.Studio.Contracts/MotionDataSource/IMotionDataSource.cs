namespace Kinemagic.Apps.Studio.Contracts.MotionDataSource
{
    public interface IMotionDataSource
    {
        DataSourceId Id { get; }
        float LastUpdatedTime { get; }
    }
}
