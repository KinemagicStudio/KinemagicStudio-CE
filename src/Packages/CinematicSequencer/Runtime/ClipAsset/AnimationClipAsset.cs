using System;
using System.Collections.Generic;
using CinematicSequencer.Animation;

namespace CinematicSequencer
{
    /// <summary>
    /// キーフレーム編集可能なアニメーションクリップのアセット。
    /// 複数のAnimationCurveを名前付きプロパティとして保持する。
    /// カメラPose、ライトProperties、ポストエフェクト等に汎用的に使用。
    /// 現行のPoseAnimation/LightPropertiesAnimationをこの1クラスに統合する。
    /// </summary>
    public sealed class AnimationClipAsset : IAnimatableClipAsset
    {
        private readonly Dictionary<string, AnimationCurve> _curves;
        private readonly AnimationPropertyDescriptor[] _propertyDescriptors;
        private AnimationFrame _cachedFrame;

        public Guid Id { get; }
        public string Name { get; set; }
        public TrackType Type { get; }
        public IReadOnlyList<AnimationPropertyDescriptor> Properties => _propertyDescriptors;

        /// <summary>
        /// プロパティ記述子の配列からClipAssetを構築。
        /// 各プロパティに対して、開始/終了キーフレーム付きのAnimationCurveが自動生成される。
        /// </summary>
        public AnimationClipAsset(TrackType type, AnimationPropertyDescriptor[] descriptors, float defaultDuration = 60f)
        {
            Id = GuidExtensions.CreateVersion7();
            Name = $"New {type} Clip";
            Type = type;
            _propertyDescriptors = descriptors;
            _curves = new Dictionary<string, AnimationCurve>(descriptors.Length);
            _cachedFrame = new AnimationFrame(descriptors.Length);

            foreach (var desc in descriptors)
            {
                _curves[desc.Name] = new AnimationCurve(new[]
                {
                    new Keyframe(0f, desc.DefaultValue),
                    new Keyframe(defaultDuration, desc.DefaultValue)
                });
            }
        }

        /// <summary>
        /// デシリアライゼーション用コンストラクタ。
        /// </summary>
        public AnimationClipAsset(Guid id, string name, TrackType type,
            AnimationPropertyDescriptor[] descriptors, Dictionary<string, AnimationCurve> curves)
        {
            Id = id;
            Name = name;
            Type = type;
            _propertyDescriptors = descriptors;
            _curves = curves;
            _cachedFrame = new AnimationFrame(descriptors.Length);
        }

        public float GetDuration()
        {
            float max = 0f;
            foreach (var curve in _curves.Values)
            {
                if (curve.Length > 0)
                    max = Math.Max(max, curve[curve.Length - 1].Time);
            }
            return max;
        }

        /// <summary>
        /// 指定時刻のアニメーション値を評価。
        /// switch文なしで全プロパティをループ処理。
        /// </summary>
        public AnimationFrame Evaluate(float time)
        {
            _cachedFrame.SetTime(time);
            for (int i = 0; i < _propertyDescriptors.Length; i++)
            {
                var name = _propertyDescriptors[i].Name;
                var value = _curves[name].Evaluate(time);
                _cachedFrame.SetProperty(i, name, value);
            }
            return _cachedFrame;
        }

        public AnimationCurve GetCurve(string propertyName)
        {
            return _curves.TryGetValue(propertyName, out var curve) ? curve : null;
        }

        public IReadOnlyList<Keyframe> GetKeyframes(string propertyName)
        {
            return GetCurve(propertyName)?.Keys ?? Array.Empty<Keyframe>();
        }

        public int AddKeyframe(string propertyName, Keyframe keyframe)
        {
            var curve = GetCurve(propertyName);
            return curve?.AddKey(keyframe) ?? -1;
        }

        public bool RemoveKeyframe(string propertyName, float time)
        {
            var curve = GetCurve(propertyName);
            return curve?.RemoveKeyAtTime(time) ?? false;
        }

        public bool UpdateKeyframeValue(string propertyName, float time, float value)
        {
            var curve = GetCurve(propertyName);
            return curve?.UpdateKeyValue(time, value) ?? false;
        }
    }
}
