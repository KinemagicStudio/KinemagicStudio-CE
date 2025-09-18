namespace Kinemagic.Apps.Studio.Contracts.SpatialEnvironment
{
    public sealed class SceneInstanceCreateCommand : IEnvironmentSystemCommand
    {
        public string ResourceKey { get; }
        public string StorageType { get; }

        public SceneInstanceCreateCommand(string resourceKey, string storageType)
        {
            ResourceKey = resourceKey;
            StorageType = storageType;
        }
    }
}
