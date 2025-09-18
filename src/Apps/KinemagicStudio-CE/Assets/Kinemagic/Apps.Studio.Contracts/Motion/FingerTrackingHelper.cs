using HumanBodyBones = UnityEngine.HumanBodyBones;

namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public static class FingerTrackingHelper
    {
        public static int GetFingerTrackingBoneId(HumanBodyBones humanBodyBoneId)
        {
            var fingerTrackingBoneId = humanBodyBoneId switch
            {
                // Left hand
                HumanBodyBones.LeftHand                => (int)FingerTrackingBones.LeftHand,
                HumanBodyBones.LeftThumbProximal       => (int)FingerTrackingBones.LeftThumbProximal,
                HumanBodyBones.LeftThumbIntermediate   => (int)FingerTrackingBones.LeftThumbIntermediate,
                HumanBodyBones.LeftThumbDistal         => (int)FingerTrackingBones.LeftThumbDistal,
                HumanBodyBones.LeftIndexProximal       => (int)FingerTrackingBones.LeftIndexProximal,
                HumanBodyBones.LeftIndexIntermediate   => (int)FingerTrackingBones.LeftIndexIntermediate,
                HumanBodyBones.LeftIndexDistal         => (int)FingerTrackingBones.LeftIndexDistal,
                HumanBodyBones.LeftMiddleProximal      => (int)FingerTrackingBones.LeftMiddleProximal,
                HumanBodyBones.LeftMiddleIntermediate  => (int)FingerTrackingBones.LeftMiddleIntermediate,
                HumanBodyBones.LeftMiddleDistal        => (int)FingerTrackingBones.LeftMiddleDistal,
                HumanBodyBones.LeftRingProximal        => (int)FingerTrackingBones.LeftRingProximal,
                HumanBodyBones.LeftRingIntermediate    => (int)FingerTrackingBones.LeftRingIntermediate,
                HumanBodyBones.LeftRingDistal          => (int)FingerTrackingBones.LeftRingDistal,
                HumanBodyBones.LeftLittleProximal      => (int)FingerTrackingBones.LeftLittleProximal,
                HumanBodyBones.LeftLittleIntermediate  => (int)FingerTrackingBones.LeftLittleIntermediate,
                HumanBodyBones.LeftLittleDistal        => (int)FingerTrackingBones.LeftLittleDistal,
                // Right hand
                HumanBodyBones.RightHand               => (int)FingerTrackingBones.RightHand,
                HumanBodyBones.RightThumbProximal      => (int)FingerTrackingBones.RightThumbProximal,
                HumanBodyBones.RightThumbIntermediate  => (int)FingerTrackingBones.RightThumbIntermediate,
                HumanBodyBones.RightThumbDistal        => (int)FingerTrackingBones.RightThumbDistal,
                HumanBodyBones.RightIndexProximal      => (int)FingerTrackingBones.RightIndexProximal,
                HumanBodyBones.RightIndexIntermediate  => (int)FingerTrackingBones.RightIndexIntermediate,
                HumanBodyBones.RightIndexDistal        => (int)FingerTrackingBones.RightIndexDistal,
                HumanBodyBones.RightMiddleProximal     => (int)FingerTrackingBones.RightMiddleProximal,
                HumanBodyBones.RightMiddleIntermediate => (int)FingerTrackingBones.RightMiddleIntermediate,
                HumanBodyBones.RightMiddleDistal       => (int)FingerTrackingBones.RightMiddleDistal,
                HumanBodyBones.RightRingProximal       => (int)FingerTrackingBones.RightRingProximal,
                HumanBodyBones.RightRingIntermediate   => (int)FingerTrackingBones.RightRingIntermediate,
                HumanBodyBones.RightRingDistal         => (int)FingerTrackingBones.RightRingDistal,
                HumanBodyBones.RightLittleProximal     => (int)FingerTrackingBones.RightLittleProximal,
                HumanBodyBones.RightLittleIntermediate => (int)FingerTrackingBones.RightLittleIntermediate,
                HumanBodyBones.RightLittleDistal       => (int)FingerTrackingBones.RightLittleDistal,
                // Others
                _ => -1,
            };
            return fingerTrackingBoneId;
        }

        public static HumanBodyBones GetHumanBodyBone(int fingerTrackingBoneId)
        {
            var humanBodyBone = fingerTrackingBoneId switch
            {
                // Left hand
                (int)FingerTrackingBones.LeftHand                => HumanBodyBones.LeftHand,
                (int)FingerTrackingBones.LeftThumbProximal       => HumanBodyBones.LeftThumbProximal,
                (int)FingerTrackingBones.LeftThumbIntermediate   => HumanBodyBones.LeftThumbIntermediate,
                (int)FingerTrackingBones.LeftThumbDistal         => HumanBodyBones.LeftThumbDistal,
                (int)FingerTrackingBones.LeftIndexProximal       => HumanBodyBones.LeftIndexProximal,
                (int)FingerTrackingBones.LeftIndexIntermediate   => HumanBodyBones.LeftIndexIntermediate,
                (int)FingerTrackingBones.LeftIndexDistal         => HumanBodyBones.LeftIndexDistal,
                (int)FingerTrackingBones.LeftMiddleProximal      => HumanBodyBones.LeftMiddleProximal,
                (int)FingerTrackingBones.LeftMiddleIntermediate  => HumanBodyBones.LeftMiddleIntermediate,
                (int)FingerTrackingBones.LeftMiddleDistal        => HumanBodyBones.LeftMiddleDistal,
                (int)FingerTrackingBones.LeftRingProximal        => HumanBodyBones.LeftRingProximal,
                (int)FingerTrackingBones.LeftRingIntermediate    => HumanBodyBones.LeftRingIntermediate,
                (int)FingerTrackingBones.LeftRingDistal          => HumanBodyBones.LeftRingDistal,
                (int)FingerTrackingBones.LeftLittleProximal      => HumanBodyBones.LeftLittleProximal,
                (int)FingerTrackingBones.LeftLittleIntermediate  => HumanBodyBones.LeftLittleIntermediate,
                (int)FingerTrackingBones.LeftLittleDistal        => HumanBodyBones.LeftLittleDistal,
                // Right hand
                (int)FingerTrackingBones.RightHand               => HumanBodyBones.RightHand,
                (int)FingerTrackingBones.RightThumbProximal      => HumanBodyBones.RightThumbProximal,
                (int)FingerTrackingBones.RightThumbIntermediate  => HumanBodyBones.RightThumbIntermediate,
                (int)FingerTrackingBones.RightThumbDistal        => HumanBodyBones.RightThumbDistal,
                (int)FingerTrackingBones.RightIndexProximal      => HumanBodyBones.RightIndexProximal,
                (int)FingerTrackingBones.RightIndexIntermediate  => HumanBodyBones.RightIndexIntermediate,
                (int)FingerTrackingBones.RightIndexDistal        => HumanBodyBones.RightIndexDistal,
                (int)FingerTrackingBones.RightMiddleProximal     => HumanBodyBones.RightMiddleProximal,
                (int)FingerTrackingBones.RightMiddleIntermediate => HumanBodyBones.RightMiddleIntermediate,
                (int)FingerTrackingBones.RightMiddleDistal       => HumanBodyBones.RightMiddleDistal,
                (int)FingerTrackingBones.RightRingProximal       => HumanBodyBones.RightRingProximal,
                (int)FingerTrackingBones.RightRingIntermediate   => HumanBodyBones.RightRingIntermediate,
                (int)FingerTrackingBones.RightRingDistal         => HumanBodyBones.RightRingDistal,
                (int)FingerTrackingBones.RightLittleProximal     => HumanBodyBones.RightLittleProximal,
                (int)FingerTrackingBones.RightLittleIntermediate => HumanBodyBones.RightLittleIntermediate,
                (int)FingerTrackingBones.RightLittleDistal       => HumanBodyBones.RightLittleDistal,
                // Others
                _ => HumanBodyBones.LastBone,
            };
            return humanBodyBone;
        }
    }
}
