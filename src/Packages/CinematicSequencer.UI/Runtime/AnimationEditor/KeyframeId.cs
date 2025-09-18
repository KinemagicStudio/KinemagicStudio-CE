using System;

namespace CinematicSequencer.UI
{
    public readonly struct KeyframeId : IEquatable<KeyframeId>
    {
        public readonly string PropertyName;
        public readonly int TimeMs;

        public float Time => TimeMs / 1000f;

        public KeyframeId(string propertyName, int timeMs)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
            }
            PropertyName = propertyName;
            TimeMs = timeMs;
        }

        public bool Equals(KeyframeId other)
        {
            return PropertyName == other.PropertyName && TimeMs == other.TimeMs;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PropertyName, TimeMs);
        }

        public static KeyframeId FromSeconds(string name, float timeSeconds)
            => new(name, (int)MathF.Round(timeSeconds * 1000f));
    }
}
