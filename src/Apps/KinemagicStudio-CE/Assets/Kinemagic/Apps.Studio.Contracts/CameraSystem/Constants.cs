using System.Collections.Generic;

namespace Kinemagic.Apps.Studio.Contracts.CameraSystem
{
    public static class Constants
    {
        public static readonly string MainCameraOutputTextureDataKey = "MainCameraOutput";
        public static readonly int MainCameraOutputTextureWidth = 1920;
        public static readonly int MainCameraOutputTextureHeight = 1080;

        public static readonly string VerticalModeMainCameraOutputTextureDataKey = "VerticalModeMainCameraOutput";
        public static readonly int VerticalModeMainCameraOutputTextureWidth = 1080;
        public static readonly int VerticalModeMainCameraOutputTextureHeight = 1920;

        public static readonly string MultiCameraViewOutputTextureDataKey = "MultiCameraViewOutput";
        public static readonly int MultiCameraViewOutputTextureWidth = 1920;
        public static readonly int MultiCameraViewOutputTextureHeight = 1080;

        public static readonly string SceneViewCameraLayerName = "SceneView";
        public static readonly Dictionary<int, string> CameraLayerNames = new()
        {
            { 1, "Camera01" },
            { 2, "Camera02" },
            { 3, "Camera03" },
            { 4, "Camera04" },
            { 5, "Camera05" },
            { 6, "Camera06" },
            { 7, "Camera07" },
            { 8, "Camera08" },
        };
    }
}