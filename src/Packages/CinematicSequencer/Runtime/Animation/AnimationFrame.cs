using System;
using System.Collections.Generic;

namespace CinematicSequencer.Animation
{
    public sealed class AnimationFrame
    {
        private readonly (string Name, float Value)[] _properties;

        public float Time { get; private set; }
        public DataType Type { get; }
        public IReadOnlyList<(string Name, float Value)> Properties => _properties;

        public AnimationFrame(DataType type, int propertyCount)
        {
            Type = type;

            if (propertyCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(propertyCount), "Property count must be greater than zero.");
            }

            _properties = new (string Name, float Value)[propertyCount];
            for (int i = 0; i < propertyCount; i++)
            {
                _properties[i] = (string.Empty, 0f);
            }
        }

        public void SetTime(float time)
        {
            Time = time;
        }

        public void SetProperty(int index, string propertyName, float value)
        {
            if (index < 0 || index >= _properties.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }
            _properties[index] = (propertyName, value);
        }

        public (string Name, float Value) GetProperty(int index)
        {
            if (index < 0 || index >= _properties.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }
            return _properties[index];
        }
    }
}
