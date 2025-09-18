using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.Contracts.Character
{
    public sealed class MotionDataSourceMappingRemoveCommand : ICharacterCommand
    {
        public readonly InstanceId ActorId;
        public readonly DataSourceId DataSourceId;
        public readonly MotionDataType MotionDataType;

        public MotionDataSourceMappingRemoveCommand(InstanceId actorId, DataSourceId dataSourceId, MotionDataType motionDataType)
        {
            ActorId = actorId;
            DataSourceId = dataSourceId;
            MotionDataType = motionDataType;
        }
    }
}