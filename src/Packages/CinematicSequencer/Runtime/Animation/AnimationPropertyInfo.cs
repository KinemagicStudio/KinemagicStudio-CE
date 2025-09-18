namespace CinematicSequencer.Animation
{
    public sealed class AnimationPropertyInfo
    {
        public string Name { get; }
        public float DefaultValue { get; }

        public AnimationPropertyInfo(string name, float defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}