using System;
using System.Collections.Generic;

namespace CinematicSequencer.Animation
{
    /// <summary>
    /// Pure C# implementation of AnimationCurve.
    /// </summary>
    [Serializable]
    public sealed class AnimationCurve
    {
        /// <summary>
        /// Determines how time is treated outside of the keyframed range of an AnimationCurve.
        /// </summary>
        public enum WrapMode
        {
            ClampForever,
        }

        protected readonly List<Keyframe> _keys = new();

        public IReadOnlyList<Keyframe> Keys => _keys;
        public int Length => _keys.Count;

        public Keyframe this[int index] => _keys[index];

        /// <summary>
        /// The behaviour of the animation before the first keyframe.
        /// </summary>
        public WrapMode PreWrapMode { get; set; } = WrapMode.ClampForever;

        /// <summary>
        /// The behaviour of the animation after the last keyframe.
        /// </summary>
        public WrapMode PostWrapMode { get; set; } = WrapMode.ClampForever;

        public AnimationCurve()
        {
        }

        public AnimationCurve(IEnumerable<Keyframe> keys)
        {
            _keys.AddRange(keys);
            _keys.Sort();
        }

        /// <summary>
        /// Adds a keyframe while preserving chronological order.
        /// If another keyframe with the exact same TimeMs already exists, the insert is skipped and -1 is returned.
        /// </summary>
        public int AddKey(Keyframe key, bool autoSmoothTangents = true)
        {
            var index = _keys.BinarySearch(key);
            if (index >= 0) return -1;

            index = ~index; // Insertion point
            _keys.Insert(index, key);

            if (autoSmoothTangents)
            {
                SmoothTangents(index);
                if (index > 0) SmoothTangents(index - 1);
                if (index < _keys.Count - 1) SmoothTangents(index + 1);
            }

            return index;
        }

        public bool UpdateKeyValue(float timeSeconds, float newValue, bool autoSmoothTangents = true)
        {
            int index = FindKeyIndex(timeSeconds);
            if (index == -1) return false;

            var existingKey = _keys[index];
            var updatedKey = new Keyframe(
                existingKey.Time,
                newValue,
                existingKey.InTangent,
                existingKey.OutTangent,
                existingKey.TangentMode
            );

            _keys[index] = updatedKey;

            if (autoSmoothTangents)
            {
                SmoothTangents(index);
                if (index > 0) SmoothTangents(index - 1);
                if (index < _keys.Count - 1) SmoothTangents(index + 1);                
            }

            return true;
        }

        public bool RemoveKey(int index, bool autoSmoothTangents = true)
        {
            // The first keyframe cannot be removed and
            // the curve must have at least two keyframes.
            if (index <= 0 || _keys.Count <= 2) return false;
            _keys.RemoveAt(index);

            if (autoSmoothTangents)
            {
                if (index > 0 && index - 1 < _keys.Count) SmoothTangents(index - 1);
                if (index < _keys.Count) SmoothTangents(index);
            }

            return true;
        }

        public bool RemoveKeyAtTime(float timeSeconds)
        {
            return RemoveKey(FindKeyIndex(timeSeconds));
        }

        public float Evaluate(float timeSeconds)
        {
            if (_keys.Count == 0) return 0f;
            if (_keys.Count == 1) return _keys[0].Value;

            var timeMs = (int)MathF.Round(timeSeconds * 1000f);

            var first = _keys[0];
            var last = _keys[^1];
            if (timeMs < first.TimeMs) return ApplyPreWrap(timeSeconds, first, last);
            if (timeMs > last.TimeMs) return ApplyPostWrap(timeSeconds, first, last);

            FindSegment(timeMs, out var low, out var high);

            var lhs = _keys[low];
            var rhs = _keys[high];

            var dt = rhs.Time - lhs.Time;
            var s = 0f;
            var m0 = 0f;
            var m1 = 0f;

            if (dt != 0f)
            {
                s = (timeSeconds - _keys[low].Time) / dt; // Normalized parameter
                m0 = lhs.OutTangent * dt;
                m1 = rhs.InTangent * dt;
            }

            return lhs.TangentMode switch
            {
                TangentMode.Free => Hermite(s, lhs.Value, rhs.Value, m0, m1),
                TangentMode.Linear => Linear(s, lhs.Value, rhs.Value),
                TangentMode.Constant => lhs.Value,
                _ => Hermite(s, lhs.Value, rhs.Value, m0, m1),
            };
        }

        /// <summary>
        /// Finds the index of the keyframe at the specified time.
        /// Returns -1 if no such key exists.
        /// </summary>
        public int FindKeyIndex(float timeSeconds)
        {
            int timeMs = (int)MathF.Round(timeSeconds * 1000f);

            // Binary search
            int low = 0;
            int high = _keys.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                int dt  = _keys[mid].TimeMs - timeMs;

                if (dt == 0) return mid;

                if (dt < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return -1;
        }

        public void FindSegment(int timeMs, out int lowIndex, out int highIndex)
        {
            // Binary search
            lowIndex = 0;
            highIndex = _keys.Count - 1;
            while (highIndex - lowIndex > 1)
            {
                int mid = (lowIndex + highIndex) >> 1;
                int dt = _keys[mid].TimeMs - timeMs;
                if (dt <= 0)
                {
                    lowIndex = mid;
                }
                else
                {
                    highIndex = mid;
                }
            }
        }

        private void SmoothTangents(int keyIndex)
        {
            if (keyIndex < 0 || keyIndex >= _keys.Count) return;

            var key = _keys[keyIndex];

            if (keyIndex > 0 && keyIndex < _keys.Count - 1)
            {
                var prevKey = _keys[keyIndex - 1];
                var nextKey = _keys[keyIndex + 1];
                var gradient = CalculateGradient(prevKey, nextKey);
                key.InTangent = gradient;
                key.OutTangent = gradient;
            }
            else if (keyIndex == 0 && _keys.Count > 1) // First keyframe
            {                
                var nextKey = _keys[1];
                var gradient = CalculateGradient(key, nextKey);
                key.InTangent = gradient;
                key.OutTangent = gradient;
            }
            else if (keyIndex == _keys.Count - 1 && _keys.Count > 1) // Last keyframe
            {
                var prevKey = _keys[_keys.Count - 2];
                var gradient = CalculateGradient(key, prevKey);
                key.InTangent = gradient;
                key.OutTangent = gradient;
            }

            _keys[keyIndex] = key;
        }

        private float CalculateGradient(Keyframe p0, Keyframe p1)
        {
            var dt = p1.Time - p0.Time;
            if (dt == 0f) return 0f;
            return (p1.Value - p0.Value) / dt;
        }

        private float ApplyPreWrap(float time, Keyframe first, Keyframe last)
        {
            return PreWrapMode switch
            {
                WrapMode.ClampForever => first.Value,
                _ => first.Value,
            };
        }

        private float ApplyPostWrap(float time, Keyframe first, Keyframe last)
        {
            return PostWrapMode switch
            {
                WrapMode.ClampForever => last.Value,
                _ => last.Value,
            };
        }

        /// <summary>
        /// Cubic Hermite Interpolator
        /// </summary>
        public static float Hermite(float s, float p0, float p1, float m0, float m1)
        {
            var s2 = s * s;
            var s3 = s2 * s;

            var a = 2f * s3 - 3f * s2 + 1f;
            var b = s3 - 2f * s2 + s;
            var c = -2f * s3 + 3f * s2;
            var d = s3 - s2;

            return a * p0 + b * m0 + c * p1 + d * m1;
        }

        /// <summary>
        /// Linear Interpolator
        /// </summary>
        public static float Linear(float s, float p0, float p1)
        {
            return p0 + s * (p1 - p0);
        }
    }
}
