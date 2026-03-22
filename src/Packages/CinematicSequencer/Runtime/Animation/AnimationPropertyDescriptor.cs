namespace CinematicSequencer.Animation
{
    /// <summary>
    /// アニメーションプロパティのメタデータ。
    /// </summary>
    public sealed class AnimationPropertyDescriptor
    {
        public string Name { get; }
        public float DefaultValue { get; }
        public float? MinValue { get; }
        public float? MaxValue { get; }
        public string DisplayName { get; }
        public string Group { get; }

        public AnimationPropertyDescriptor(
            string name, float defaultValue,
            string displayName = null, string group = null,
            float? minValue = null, float? maxValue = null)
        {
            Name = name;
            DefaultValue = defaultValue;
            DisplayName = displayName ?? name;
            Group = group;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}
