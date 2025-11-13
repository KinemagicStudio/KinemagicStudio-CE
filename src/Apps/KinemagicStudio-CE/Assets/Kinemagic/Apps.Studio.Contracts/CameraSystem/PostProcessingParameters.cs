using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public interface IPostProcessingParameters
    {
    }

    public sealed class ColorAdjustmentParameters : IPostProcessingParameters
    {
        public bool IsEnabled { get; set; }
        public float PostExposure { get; set; } = 0f; // -10f to 10f
        public float Contrast { get; set; } = 0f;
        public float HueShift { get; set; } = 0f;
        public float Saturation { get; set; } = 0f;
    }

    public sealed class BokehDepthOfFieldParameters : IPostProcessingParameters
    {
        public bool IsEnabled { get; set; }
        public int BladeCount { get; set; } = 5;
        public float BladeCurvature { get; set; } = 1f;
        public float BladeRotation { get; set; } = 0f;
        public float FocusDistance { get; set; } = 10f;
        public float Aperture { get; set; } = 5.6f;
    }

    public sealed class TonemappingParameters : IPostProcessingParameters
    {
        public bool IsEnabled { get; set; }
        public TonemappingMode Mode { get; set; } = TonemappingMode.Neutral;
    }

    public enum TonemappingMode
    {
        None,
        Neutral,
        ACES
    }

    public sealed class BloomParameters : IPostProcessingParameters
    {
        public bool IsEnabled { get; set; }
        public float Threshold { get; set; } = 0.9f;
        public float Intensity { get; set; } = 0f;
        public float Scatter { get; set; } = 0.7f;
    }

    public sealed class ScreenSpaceLensFlareParameters : IPostProcessingParameters
    {
        public bool IsEnabled { get; set; }
        public float Intensity { get; set; } = 0f;
        public float RegularMultiplier { get; set; } = 1f;
        public float ReversedMultiplier { get; set; } = 1f;
        public float WarpedMultiplier { get; set; } = 1f;
        public float StreaksMultiplier { get; set; } = 1f;
        public float StreaksLength { get; set; } = 1f;
        public float StreaksThreshold { get; set; } = 0.5f;
        public float StreaksOrientation { get; set; } = 0f;
        public float ChromaticAberrationIntensity { get; set; } = 0f;
    }

    public sealed class ScreenEdgeColorParameters : IPostProcessingParameters
    {
        public bool IsEnabled { get; set; }
        public float Intensity { get; set; } = 0f;
        public Vector4 TopLeftColor { get; set; } = new(0, 1, 1, 1);     // Cyan
        public Vector4 TopRightColor { get; set; } = new(1, 0, 1, 1);    // Magenta
        public Vector4 BottomLeftColor { get; set; } = new(1, 1, 0, 1);  // Yellow
        public Vector4 BottomRightColor { get; set; } = new(1, 0, 0, 1); // Red
    }
}
