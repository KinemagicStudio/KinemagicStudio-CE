namespace Kinemagic.Apps.Studio.Contracts.Character
{
    public sealed class CharacterInstanceInfo
    {
        public InstanceId InstanceId { get; }
        public string Name { get; }

        public CharacterInstanceInfo(InstanceId instanceId, string name)
        {
            InstanceId = instanceId;
            Name = name;
        }
    }
}
