namespace Kinemagic.Apps.Studio.Contracts.Character
{
    public sealed class CharacterInstanceCreateCommand : ICharacterCommand
    {
        public string ResourceKey { get; }
        public string StorageType { get; }

        public CharacterInstanceCreateCommand(string resourceKey, string storageType)
        {
            ResourceKey = resourceKey;
            StorageType = storageType;
        }
    }
}
