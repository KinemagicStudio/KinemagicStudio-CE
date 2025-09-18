using System;
using System.Collections.Generic;
using System.Numerics;
using CinematicSequencer.Animation;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace CinematicSequencer
{
    [Serializable]
    public sealed class PoseAnimation : IAnimationClipData
    {
        public const float DefaultDuration = 60f;

        public static class PropertyNames
        {
            public const string PositionX = "PositionX";
            public const string PositionY = "PositionY";
            public const string PositionZ = "PositionZ";
            public const string EulerAngleX = "EulerAngleX";
            public const string EulerAngleY = "EulerAngleY";
            public const string EulerAngleZ = "EulerAngleZ";
        }

        public static class PropertyDefaultValues
        {
            public const float PositionX = 0f;
            public const float PositionY = 0f;
            public const float PositionZ = 0f;
            public const float EulerAngleX = 0f;
            public const float EulerAngleY = 0f;
            public const float EulerAngleZ = 0f;
        }

        public static readonly AnimationPropertyInfo[] Properties = new AnimationPropertyInfo[]
        {
            new AnimationPropertyInfo(PropertyNames.PositionX, PropertyDefaultValues.PositionX),
            new AnimationPropertyInfo(PropertyNames.PositionY, PropertyDefaultValues.PositionY),
            new AnimationPropertyInfo(PropertyNames.PositionZ, PropertyDefaultValues.PositionZ),
            new AnimationPropertyInfo(PropertyNames.EulerAngleX, PropertyDefaultValues.EulerAngleX),
            new AnimationPropertyInfo(PropertyNames.EulerAngleY, PropertyDefaultValues.EulerAngleY),
            new AnimationPropertyInfo(PropertyNames.EulerAngleZ, PropertyDefaultValues.EulerAngleZ)
        };

        private readonly AnimationCurve _positionX = new(new[]
        {
            new Keyframe(0f, PropertyDefaultValues.PositionX),
            new Keyframe(DefaultDuration, PropertyDefaultValues.PositionX)
        });
        private readonly AnimationCurve _positionY = new(new[]
        {
            new Keyframe(0f, PropertyDefaultValues.PositionY),
            new Keyframe(DefaultDuration, PropertyDefaultValues.PositionY)
        });
        private readonly AnimationCurve _positionZ = new(new[]
        {
            new Keyframe(0f, PropertyDefaultValues.PositionZ),
            new Keyframe(DefaultDuration, PropertyDefaultValues.PositionZ)
        });
        private readonly AnimationCurve _eulerAngleX = new(new[]
        {
            new Keyframe(0f, PropertyDefaultValues.EulerAngleX),
            new Keyframe(DefaultDuration, PropertyDefaultValues.EulerAngleX)
        });
        private readonly AnimationCurve _eulerAngleY = new(new[]
        {
            new Keyframe(0f, PropertyDefaultValues.EulerAngleY),
            new Keyframe(DefaultDuration, PropertyDefaultValues.EulerAngleY)
        });
        private readonly AnimationCurve _eulerAngleZ = new(new[]
        {
            new Keyframe(0f, PropertyDefaultValues.EulerAngleZ),
            new Keyframe(DefaultDuration, PropertyDefaultValues.EulerAngleZ)
        });

        private readonly (string PropertyName, Keyframe? Keyframe)[] _keyframes = new (string PropertyName, Keyframe? Keyframe)[Properties.Length];
        private readonly AnimationFrame _currentFrame;

        public string CinematicSequencerFormatVersion => Constants.CinematicSequencerFormatVersion;
        
        public Guid Id { get; }
        public string Name { get; set; }
        public DataType Type { get; }

        public AnimationCurve PositionX => _positionX;
        public AnimationCurve PositionY => _positionY;
        public AnimationCurve PositionZ => _positionZ;
        public AnimationCurve EulerAngleX => _eulerAngleX;
        public AnimationCurve EulerAngleY => _eulerAngleY;
        public AnimationCurve EulerAngleZ => _eulerAngleZ;

        public PoseAnimation(DataType type)
        {
            if (type != DataType.CameraPose && type != DataType.LightPose)
            {
                throw new ArgumentException($"Invalid data type: {type}", nameof(type));
            }

            Id = GuidExtensions.CreateVersion7();
            Name = $"{type}_{DateTime.Now:yyyyMMdd_HHmmss}";
            Type = type;

            _currentFrame = new AnimationFrame(type, Properties.Length);
        }

        [JsonConstructor]
        public PoseAnimation(
            string id,
            string name,
            DataType type,
            AnimationCurve positionX,
            AnimationCurve positionY,
            AnimationCurve positionZ,
            AnimationCurve eulerAngleX,
            AnimationCurve eulerAngleY,
            AnimationCurve eulerAngleZ)
        {
            if (type != DataType.CameraPose && type != DataType.LightPose)
            {
                throw new ArgumentException($"Invalid data type: {type}", nameof(type));
            }

            Id = new Guid(id);
            Name = name;
            Type = type;

            _positionX = positionX;
            _positionY = positionY;
            _positionZ = positionZ;
            _eulerAngleX = eulerAngleX;
            _eulerAngleY = eulerAngleY;
            _eulerAngleZ = eulerAngleZ;

            _currentFrame = new AnimationFrame(type, Properties.Length);
        }
        
        public AnimationPropertyInfo[] GetProperties()
        {
            return Properties;
        }
        
        public float GetDuration()
        {
            var duration = 0f;

            duration = PositionX.Length <= 0 ? duration : Math.Max(duration, PositionX[^1].Time);
            duration = PositionY.Length <= 0 ? duration : Math.Max(duration, PositionY[^1].Time);
            duration = PositionZ.Length <= 0 ? duration : Math.Max(duration, PositionZ[^1].Time);

            duration = EulerAngleX.Length <= 0 ? duration : Math.Max(duration, EulerAngleX[^1].Time);
            duration = EulerAngleY.Length <= 0 ? duration : Math.Max(duration, EulerAngleY[^1].Time);
            duration = EulerAngleZ.Length <= 0 ? duration : Math.Max(duration, EulerAngleZ[^1].Time);

            return duration;
        }

        /// <summary>
        /// Frequently called method
        /// </summary>
        public AnimationFrame Evaluate(float time)
        {
            var posX = PositionX.Evaluate(time);
            var posY = PositionY.Evaluate(time);
            var posZ = PositionZ.Evaluate(time);
            var rotX = EulerAngleX.Evaluate(time);
            var rotY = EulerAngleY.Evaluate(time);
            var rotZ = EulerAngleZ.Evaluate(time);

            _currentFrame.SetTime(time);
            _currentFrame.SetProperty(0, PropertyNames.PositionX, posX);
            _currentFrame.SetProperty(1, PropertyNames.PositionY, posY);
            _currentFrame.SetProperty(2, PropertyNames.PositionZ, posZ);
            _currentFrame.SetProperty(3, PropertyNames.EulerAngleX, rotX);
            _currentFrame.SetProperty(4, PropertyNames.EulerAngleY, rotY);
            _currentFrame.SetProperty(5, PropertyNames.EulerAngleZ, rotZ);

            return _currentFrame;
        }

        public IReadOnlyList<(string PropertyName, Keyframe? Keyframe)> GetKeyframes(float time)
        {
            var index = PositionX.FindKeyIndex(time);
            _keyframes[0] = (index >= 0) ? (PropertyNames.PositionX, PositionX[index]) : (PropertyNames.PositionX, null);

            index = PositionY.FindKeyIndex(time);
            _keyframes[1] = (index >= 0) ? (PropertyNames.PositionY, PositionY[index]) : (PropertyNames.PositionY, null);

            index = PositionZ.FindKeyIndex(time);
            _keyframes[2] = (index >= 0) ? (PropertyNames.PositionZ, PositionZ[index]) : (PropertyNames.PositionZ, null);

            index = EulerAngleX.FindKeyIndex(time);
            _keyframes[3] = (index >= 0) ? (PropertyNames.EulerAngleX, EulerAngleX[index]) : (PropertyNames.EulerAngleX, null);

            index = EulerAngleY.FindKeyIndex(time);
            _keyframes[4] = (index >= 0) ? (PropertyNames.EulerAngleY, EulerAngleY[index]) : (PropertyNames.EulerAngleY, null);

            index = EulerAngleZ.FindKeyIndex(time);
            _keyframes[5] = (index >= 0) ? (PropertyNames.EulerAngleZ, EulerAngleZ[index]) : (PropertyNames.EulerAngleZ, null);

            return _keyframes;
        }

        public IReadOnlyList<Keyframe> GetKeyframes(string propertyName)
        {
            switch (propertyName)
            {
                case PropertyNames.PositionX:
                    return PositionX.Keys;
                case PropertyNames.PositionY:
                    return PositionY.Keys;
                case PropertyNames.PositionZ:
                    return PositionZ.Keys;
                case PropertyNames.EulerAngleX:
                    return EulerAngleX.Keys;
                case PropertyNames.EulerAngleY:
                    return EulerAngleY.Keys;
                case PropertyNames.EulerAngleZ:
                    return EulerAngleZ.Keys;
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
                case PropertyNames.PositionX:
                    index = PositionX.FindKeyIndex(time);
                    if (index >= 0) keyframe = PositionX[index];
                    break;
                case PropertyNames.PositionY:
                    index = PositionY.FindKeyIndex(time);
                    if (index >= 0) keyframe = PositionY[index];
                    break;
                case PropertyNames.PositionZ:
                    index = PositionZ.FindKeyIndex(time);
                    if (index >= 0) keyframe = PositionZ[index];
                    break;
                case PropertyNames.EulerAngleX:
                    index = EulerAngleX.FindKeyIndex(time);
                    if (index >= 0) keyframe = EulerAngleX[index];
                    break;
                case PropertyNames.EulerAngleY:
                    index = EulerAngleY.FindKeyIndex(time);
                    if (index >= 0) keyframe = EulerAngleY[index];
                    break;
                case PropertyNames.EulerAngleZ:
                    index = EulerAngleZ.FindKeyIndex(time);
                    if (index >= 0) keyframe = EulerAngleZ[index];
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
                case PropertyNames.PositionX:
                    index = PositionX.AddKey(keyframe);
                    break;
                case PropertyNames.PositionY:
                    index = PositionY.AddKey(keyframe);
                    break;
                case PropertyNames.PositionZ:
                    index = PositionZ.AddKey(keyframe);
                    break;
                case PropertyNames.EulerAngleX:
                    index = EulerAngleX.AddKey(keyframe);
                    break;
                case PropertyNames.EulerAngleY:
                    index = EulerAngleY.AddKey(keyframe);
                    break;
                case PropertyNames.EulerAngleZ:
                    index = EulerAngleZ.AddKey(keyframe);
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
                case PropertyNames.PositionX:
                    return PositionX.RemoveKeyAtTime(time);
                case PropertyNames.PositionY:
                    return PositionY.RemoveKeyAtTime(time);
                case PropertyNames.PositionZ:
                    return PositionZ.RemoveKeyAtTime(time);
                case PropertyNames.EulerAngleX:
                    return EulerAngleX.RemoveKeyAtTime(time);
                case PropertyNames.EulerAngleY:
                    return EulerAngleY.RemoveKeyAtTime(time);
                case PropertyNames.EulerAngleZ:
                    return EulerAngleZ.RemoveKeyAtTime(time);
                default:
                    throw new ArgumentException($"Invalid property name: {propertyName}", nameof(propertyName));
            }
        }

        public bool UpdateKeyframeValue(string propertyName, float time, float value)
        {
            switch (propertyName)
            {
                case PropertyNames.PositionX:
                    return PositionX.UpdateKeyValue(time, value);
                case PropertyNames.PositionY:
                    return PositionY.UpdateKeyValue(time, value);
                case PropertyNames.PositionZ:
                    return PositionZ.UpdateKeyValue(time, value);
                case PropertyNames.EulerAngleX:
                    return EulerAngleX.UpdateKeyValue(time, value);
                case PropertyNames.EulerAngleY:
                    return EulerAngleY.UpdateKeyValue(time, value);
                case PropertyNames.EulerAngleZ:
                    return EulerAngleZ.UpdateKeyValue(time, value);
                default:
                    throw new ArgumentException($"Invalid property name: {propertyName}", nameof(propertyName));
            }
        }
    }
}
