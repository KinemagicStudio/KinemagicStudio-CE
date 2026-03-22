using System;

namespace CinematicSequencer.Animation
{
    /// <summary>
    /// よく使うプロパティセットのテンプレート。
    /// </summary>
    public static class PropertyTemplates
    {
        public static AnimationPropertyDescriptor[] CreatePoseProperties() => new[]
        {
            new AnimationPropertyDescriptor("PositionX", 0f, "X", "Position"),
            new AnimationPropertyDescriptor("PositionY", 0f, "Y", "Position"),
            new AnimationPropertyDescriptor("PositionZ", 0f, "Z", "Position"),
            new AnimationPropertyDescriptor("EulerAngleX", 0f, "X", "Rotation"),
            new AnimationPropertyDescriptor("EulerAngleY", 0f, "Y", "Rotation"),
            new AnimationPropertyDescriptor("EulerAngleZ", 0f, "Z", "Rotation"),
        };

        public static AnimationPropertyDescriptor[] CreateLightProperties() => new[]
        {
            new AnimationPropertyDescriptor("ColorR", 1f, "R", "Color", 0f, 1f),
            new AnimationPropertyDescriptor("ColorG", 1f, "G", "Color", 0f, 1f),
            new AnimationPropertyDescriptor("ColorB", 1f, "B", "Color", 0f, 1f),
            new AnimationPropertyDescriptor("Intensity", 1f, "Intensity", null, 0f, null),
            new AnimationPropertyDescriptor("Range", 10f, "Range", null, 0f, null),
        };

        /// <summary>
        /// ScreenEdgeColorエフェクトのプロパティ。
        /// KinemagicRenderPipelineのScreenEdgeColor VolumeComponentに対応。
        /// </summary>
        public static AnimationPropertyDescriptor[] CreateScreenEdgeColorProperties() => new[]
        {
            new AnimationPropertyDescriptor("Intensity", 0f, "Intensity", null, 0f, 1f),
            new AnimationPropertyDescriptor("TopLeftColorR", 0f, "R", "TopLeftColor", 0f, 1f),
            new AnimationPropertyDescriptor("TopLeftColorG", 1f, "G", "TopLeftColor", 0f, 1f),
            new AnimationPropertyDescriptor("TopLeftColorB", 1f, "B", "TopLeftColor", 0f, 1f),
            new AnimationPropertyDescriptor("TopRightColorR", 1f, "R", "TopRightColor", 0f, 1f),
            new AnimationPropertyDescriptor("TopRightColorG", 0f, "G", "TopRightColor", 0f, 1f),
            new AnimationPropertyDescriptor("TopRightColorB", 1f, "B", "TopRightColor", 0f, 1f),
            new AnimationPropertyDescriptor("BottomLeftColorR", 1f, "R", "BottomLeftColor", 0f, 1f),
            new AnimationPropertyDescriptor("BottomLeftColorG", 1f, "G", "BottomLeftColor", 0f, 1f),
            new AnimationPropertyDescriptor("BottomLeftColorB", 0f, "B", "BottomLeftColor", 0f, 1f),
            new AnimationPropertyDescriptor("BottomRightColorR", 1f, "R", "BottomRightColor", 0f, 1f),
            new AnimationPropertyDescriptor("BottomRightColorG", 0f, "G", "BottomRightColor", 0f, 1f),
            new AnimationPropertyDescriptor("BottomRightColorB", 0f, "B", "BottomRightColor", 0f, 1f),
        };

        /// <summary>
        /// テンプレートからClipAssetを生成するファクトリ。
        /// </summary>
        public static AnimationClipAsset CreateClipAsset(TrackType type)
        {
            var descriptors = type switch
            {
                TrackType.CameraPose => CreatePoseProperties(),
                TrackType.LightPose => CreatePoseProperties(),
                TrackType.LightProperties => CreateLightProperties(),
                _ => throw new NotSupportedException($"No template for {type}")
            };
            return new AnimationClipAsset(type, descriptors);
        }

        /// <summary>
        /// ポストエフェクト用: エフェクト名を指定してClipAssetを生成。
        /// 新しいポストエフェクトはここにテンプレートを追加するだけで対応可能。
        /// </summary>
        public static AnimationClipAsset CreatePostEffectClipAsset(string effectName)
        {
            var descriptors = effectName switch
            {
                "ScreenEdgeColor" => CreateScreenEdgeColorProperties(),
                _ => throw new NotSupportedException($"No template for post-effect: {effectName}")
            };
            return new AnimationClipAsset(TrackType.PostEffect, descriptors);
        }
    }
}
