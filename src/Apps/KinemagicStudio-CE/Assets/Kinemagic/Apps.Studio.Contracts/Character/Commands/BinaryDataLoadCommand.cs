namespace Kinemagic.Apps.Studio.Contracts.Character
{
    public sealed class BinaryDataLoadCommand
    {
        public string Key { get; }
        public BinaryDataStorageType DataStorageType { get; }

        public BinaryDataLoadCommand(string key, BinaryDataStorageType dataStorageType)
        {
            Key = key;
            DataStorageType = dataStorageType;
        }
    }
}
