namespace Kinemagic.Apps.Studio.Contracts.SpatialEnvironment
{
    public sealed class EnvironmentModelInfo
    {
        public string ResourceKey { get; }
        public string StorageType { get; }
        public string DisplayName { get; }

        public EnvironmentModelInfo(string resourceKey, string storageType, string displayName)
        {
            ResourceKey = resourceKey;
            StorageType = storageType;
            DisplayName = displayName;
        }
    }
}
