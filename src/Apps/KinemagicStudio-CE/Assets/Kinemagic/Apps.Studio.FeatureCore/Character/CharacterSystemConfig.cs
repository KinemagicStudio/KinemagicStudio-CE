namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class CharacterSystemConfig
    {
        public string PersistentDataPath { get; }
        public string StreamingAssetsPath { get; }

        public CharacterSystemConfig(string persistentDataPath, string streamingAssetsPath)
        {
            PersistentDataPath = persistentDataPath;
            StreamingAssetsPath = streamingAssetsPath;
        }
    }
}