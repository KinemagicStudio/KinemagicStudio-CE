using System;
using System.Collections.Generic;
using CinematicSequencer.Animation;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    [Serializable]
    public sealed class LightPropertiesAnimation : IAnimationClipData
    {
        public const float DefaultDuration = 60f;
        public static DataType DataType = DataType.LightProperties;

        public static class PropertyNames
        {
            public const string ColorR = "ColorR";
            public const string ColorG = "ColorG";
            public const string ColorB = "ColorB";
            public const string Intensity = "Intensity";
            public const string Range = "Range";
        }

        public static class DefaultValues
        {
            public const float ColorR = 1f;
            public const float ColorG = 1f;
            public const float ColorB = 1f;
            public const float Intensity = 1f;
            public const float Range = 10f;
        }

        public static readonly AnimationPropertyInfo[] Properties = new AnimationPropertyInfo[]
        {
            new AnimationPropertyInfo(PropertyNames.ColorR, DefaultValues.ColorR),
            new AnimationPropertyInfo(PropertyNames.ColorG, DefaultValues.ColorG),
            new AnimationPropertyInfo(PropertyNames.ColorB, DefaultValues.ColorB),
            new AnimationPropertyInfo(PropertyNames.Intensity, DefaultValues.Intensity),
            new AnimationPropertyInfo(PropertyNames.Range, DefaultValues.Range)
        };

        private readonly AnimationCurve _colorR = new(new[]
        {
            new Keyframe(0f, DefaultValues.ColorR),
            new Keyframe(DefaultDuration, DefaultValues.ColorR)
        });
        private readonly AnimationCurve _colorG = new(new[]
        {
            new Keyframe(0f, DefaultValues.ColorG),
            new Keyframe(DefaultDuration, DefaultValues.ColorG)
        });
        private readonly AnimationCurve _colorB = new(new[]
        {
            new Keyframe(0f, DefaultValues.ColorB),
            new Keyframe(DefaultDuration, DefaultValues.ColorB)
        });
        private readonly AnimationCurve _intensity = new(new[]
        {
            new Keyframe(0f, DefaultValues.Intensity),
            new Keyframe(DefaultDuration, DefaultValues.Intensity)
        });
        private readonly AnimationCurve _range = new(new[]
        {
            new Keyframe(0f, DefaultValues.Range),
            new Keyframe(DefaultDuration, DefaultValues.Range)
        });

        private readonly (string PropertyName, Keyframe? Keyframe)[] _keyframes = new (string PropertyName, Keyframe? Keyframe)[Properties.Length];
        private readonly AnimationFrame _currentFrame = new(DataType, Properties.Length);

        public string CinematicSequencerFormatVersion => Constants.CinematicSequencerFormatVersion;

        public Guid Id { get; }
        public string Name { get; set; }
        public DataType Type => DataType;

        public AnimationCurve ColorR => _colorR;
        public AnimationCurve ColorG => _colorG;
        public AnimationCurve ColorB => _colorB;
        public AnimationCurve Intensity => _intensity;
        public AnimationCurve Range => _range;
        
        public LightPropertiesAnimation()
        {
            Name = $"LightProperties_{DateTime.Now:yyyyMMdd_HHmmss}";
            Id = GuidExtensions.CreateVersion7();
        }

        [JsonConstructor]
        public LightPropertiesAnimation(
            string id,
            string name,
            AnimationCurve colorR,
            AnimationCurve colorG,
            AnimationCurve colorB,
            AnimationCurve intensity,
            AnimationCurve range)
        {
            Id = new Guid(id);
            Name = name;
            _colorR = colorR;
            _colorG = colorG;
            _colorB = colorB;
            _intensity = intensity;
            _range = range;
        }

        public AnimationPropertyInfo[] GetProperties()
        {
            return Properties;
        }

        public float GetDuration()
        {
            var duration = 0f;

            duration = ColorR.Length <= 0 ? duration : Math.Max(duration, ColorR[^1].Time);
            duration = ColorG.Length <= 0 ? duration : Math.Max(duration, ColorG[^1].Time);
            duration = ColorB.Length <= 0 ? duration : Math.Max(duration, ColorB[^1].Time);
            
            duration = Intensity.Length <= 0 ? duration : Math.Max(duration, Intensity[^1].Time);
            duration = Range.Length     <= 0 ? duration : Math.Max(duration, Range[^1].Time);

            return duration;
        }

        /// <summary>
        /// Frequently called method
        /// </summary>
        public AnimationFrame Evaluate(float time)
        {
            var colorR = ColorR.Evaluate(time);
            var colorG = ColorG.Evaluate(time);
            var colorB = ColorB.Evaluate(time);
            var intensity = Intensity.Evaluate(time);
            var range = Range.Evaluate(time);

            _currentFrame.SetTime(time);
            _currentFrame.SetProperty(0, PropertyNames.ColorR, colorR);
            _currentFrame.SetProperty(1, PropertyNames.ColorG, colorG);
            _currentFrame.SetProperty(2, PropertyNames.ColorB, colorB);
            _currentFrame.SetProperty(3, PropertyNames.Intensity, intensity);
            _currentFrame.SetProperty(4, PropertyNames.Range, range);

            return _currentFrame;
        }

        public IReadOnlyList<(string PropertyName, Keyframe? Keyframe)> GetKeyframes(float time)
        {
            var index = ColorR.FindKeyIndex(time);
            _keyframes[0] = (index >= 0) ? (PropertyNames.ColorR, ColorR[index]) : (PropertyNames.ColorR, null);
            
            index = ColorG.FindKeyIndex(time);
            _keyframes[1] = (index >= 0) ? (PropertyNames.ColorG, ColorG[index]) : (PropertyNames.ColorG, null);
            
            index = ColorB.FindKeyIndex(time);
            _keyframes[2] = (index >= 0) ? (PropertyNames.ColorB, ColorB[index]) : (PropertyNames.ColorB, null);
            
            index = Intensity.FindKeyIndex(time);
            _keyframes[3] = (index >= 0) ? (PropertyNames.Intensity, Intensity[index]) : (PropertyNames.Intensity, null);
            
            index = Range.FindKeyIndex(time);
            _keyframes[4] = (index >= 0) ? (PropertyNames.Range, Range[index]) : (PropertyNames.Range, null);

            return _keyframes;
        }

        public IReadOnlyList<Keyframe> GetKeyframes(string propertyName)
        {
            switch (propertyName)
            {
                case PropertyNames.ColorR:
                    return ColorR.Keys;
                case PropertyNames.ColorG:
                    return ColorG.Keys;
                case PropertyNames.ColorB:
                    return ColorB.Keys;
                case PropertyNames.Intensity:
                    return Intensity.Keys;
                case PropertyNames.Range:
                    return Range.Keys;
                default:
                    return Array.Empty<Keyframe>();
            }
        }

        public bool TryGetKeyframe(string propertyName, float time, out Keyframe keyframe)
        {
            keyframe = default;

            var index = -1;
            switch (propertyName)
            {
                case PropertyNames.ColorR:
                    index = ColorR.FindKeyIndex(time);
                    if (index >= 0) keyframe = ColorR[index];
                    break;
                case PropertyNames.ColorG:
                    index = ColorG.FindKeyIndex(time);
                    if (index >= 0) keyframe = ColorG[index];
                    break;
                case PropertyNames.ColorB:
                    index = ColorB.FindKeyIndex(time);
                    if (index >= 0) keyframe = ColorB[index];
                    break;
                case PropertyNames.Intensity:
                    index = Intensity.FindKeyIndex(time);
                    if (index >= 0) keyframe = Intensity[index];
                    break;
                case PropertyNames.Range:
                    index = Range.FindKeyIndex(time);
                    if (index >= 0) keyframe = Range[index];
                    break;
                default:
                    return false;
            }

            return index >= 0;
        }

        public int TryAddKeyframe(string propertyName, Keyframe keyframe)
        {
            var index = -1;

            switch (propertyName)
            {
                case PropertyNames.ColorR:
                    index = ColorR.AddKey(keyframe);
                    break;
                case PropertyNames.ColorG:
                    index = ColorG.AddKey(keyframe);
                    break;
                case PropertyNames.ColorB:
                    index = ColorB.AddKey(keyframe);
                    break;
                case PropertyNames.Intensity:
                    index = Intensity.AddKey(keyframe);
                    break;
                case PropertyNames.Range:
                    index = Range.AddKey(keyframe);
                    break;
                default:
                    throw new ArgumentException($"Invalid property name: {propertyName}", nameof(propertyName));
            }

            return index;
        }

        public bool RemoveKeyframe(string propertyName, float time)
        {
            switch (propertyName)
            {
                case PropertyNames.ColorR:
                    return ColorR.RemoveKeyAtTime(time);
                case PropertyNames.ColorG:
                    return ColorG.RemoveKeyAtTime(time);
                case PropertyNames.ColorB:
                    return ColorB.RemoveKeyAtTime(time);
                case PropertyNames.Intensity:
                    return Intensity.RemoveKeyAtTime(time);
                case PropertyNames.Range:
                    return Range.RemoveKeyAtTime(time);
                default:
                    throw new ArgumentException($"Invalid property name: {propertyName}", nameof(propertyName));
            }
        }

        public bool UpdateKeyframeValue(string propertyName, float time, float value)
        {
            switch (propertyName)
            {
                case PropertyNames.ColorR:
                    return ColorR.UpdateKeyValue(time, value);
                case PropertyNames.ColorG:
                    return ColorG.UpdateKeyValue(time, value);
                case PropertyNames.ColorB:
                    return ColorB.UpdateKeyValue(time, value);
                case PropertyNames.Intensity:
                    return Intensity.UpdateKeyValue(time, value);
                case PropertyNames.Range:
                    return Range.UpdateKeyValue(time, value);
                default:
                    throw new ArgumentException($"Invalid property name: {propertyName}", nameof(propertyName));
            }
        }
    }
}
