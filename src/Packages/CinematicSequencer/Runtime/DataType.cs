using System;

namespace CinematicSequencer
{
    [Serializable]
    public enum DataType
    {
        Unknown,
        CameraPose,
        LightPose,
        CameraProperties,
        LightProperties,
        // PostEffect,
        Effect,
        Audio,
    }
}