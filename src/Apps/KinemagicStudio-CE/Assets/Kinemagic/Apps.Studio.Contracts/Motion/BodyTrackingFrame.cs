using System;
using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class BodyTrackingFrame
    {
        public int BoneCount { get; }

        public Vector3 Scale { get; set; }

        public Vector3 RootPosition { get; set; }
        public Quaternion RootRotation { get; set; }

        public Vector3[] BonePositions { get; }
        public Quaternion[] BoneRotations { get; }

        public BodyTrackingFrame()
        {
            BoneCount = Enum.GetValues(typeof(BodyTrackingBones)).Length;
            Scale = Vector3.One;
            RootPosition = Vector3.Zero;
            RootRotation = Quaternion.Identity;
            BonePositions = new Vector3[BoneCount];
            BoneRotations = new Quaternion[BoneCount];
        }
    }
}
