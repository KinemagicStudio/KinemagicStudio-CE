using System;
using System.Numerics;

namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class FingerTrackingFrame
    {
        public int BoneCount { get; }
        public Quaternion[] BoneRotations { get; }

        public FingerTrackingFrame()
        {
            BoneCount = Enum.GetValues(typeof(FingerTrackingBones)).Length;
            BoneRotations = new Quaternion[BoneCount];
        }
    }
}
