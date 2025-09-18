using System;
using System.Collections.Generic;
using CinematicSequencer.Animation;

namespace CinematicSequencer
{
    public interface IClipData
    {
        string CinematicSequencerFormatVersion { get; }
        Guid Id { get; }
        string Name { get; set; }
        DataType Type { get; }
        float GetDuration();
    }

    public interface IAnimationClipData : IClipData
    {
        AnimationPropertyInfo[] GetProperties();
        AnimationFrame Evaluate(float time);
        IReadOnlyList<Keyframe> GetKeyframes(string propertyName);
        IReadOnlyList<(string PropertyName, Keyframe? Keyframe)> GetKeyframes(float time);
        bool TryGetKeyframe(string propertyName, float time, out Keyframe keyframe);
        int TryAddKeyframe(string propertyName, Keyframe keyframe);
        bool RemoveKeyframe(string propertyName, float time);
        bool UpdateKeyframeValue(string propertyName, float time, float value);
    }
}