using System.Collections.Generic;
using CinematicSequencer.Animation;

namespace CinematicSequencer
{
    /// <summary>
    /// キーフレーム編集可能なクリップアセット。
    /// シーケンサー内蔵のAnimationCurveでプロパティ値を補間する。
    /// カメラPose、ライトProperties、ポストエフェクト等が該当。
    /// </summary>
    public interface IAnimatableClipAsset : IClipAsset
    {
        IReadOnlyList<AnimationPropertyDescriptor> Properties { get; }
        AnimationFrame Evaluate(float time);
        AnimationCurve GetCurve(string propertyName);
        IReadOnlyList<Keyframe> GetKeyframes(string propertyName);
        int AddKeyframe(string propertyName, Keyframe keyframe);
        bool RemoveKeyframe(string propertyName, float time);
        bool UpdateKeyframeValue(string propertyName, float time, float value);
    }
}
