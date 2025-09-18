using HumanBodyBones = UnityEngine.HumanBodyBones;

namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public static class BodyTrackingHelper
    {
        public static int GetBodyTrackingBoneId(HumanBodyBones humanBodyBoneId)
        {
            var bodyTrackingBoneId = humanBodyBoneId switch
            {
                HumanBodyBones.Hips          => (int)BodyTrackingBones.Hips,
                HumanBodyBones.Spine         => (int)BodyTrackingBones.Spine,
                HumanBodyBones.Chest         => (int)BodyTrackingBones.Chest,
                HumanBodyBones.UpperChest    => (int)BodyTrackingBones.UpperChest,
                HumanBodyBones.Neck          => (int)BodyTrackingBones.Neck,
                HumanBodyBones.Head          => (int)BodyTrackingBones.Head,
                HumanBodyBones.LeftShoulder  => (int)BodyTrackingBones.LeftShoulder,
                HumanBodyBones.LeftUpperArm  => (int)BodyTrackingBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm  => (int)BodyTrackingBones.LeftLowerArm,
                HumanBodyBones.LeftHand      => (int)BodyTrackingBones.LeftHand,
                HumanBodyBones.RightShoulder => (int)BodyTrackingBones.RightShoulder,
                HumanBodyBones.RightUpperArm => (int)BodyTrackingBones.RightUpperArm,
                HumanBodyBones.RightLowerArm => (int)BodyTrackingBones.RightLowerArm,
                HumanBodyBones.RightHand     => (int)BodyTrackingBones.RightHand,
                HumanBodyBones.LeftUpperLeg  => (int)BodyTrackingBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg  => (int)BodyTrackingBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot      => (int)BodyTrackingBones.LeftFoot,
                HumanBodyBones.LeftToes      => (int)BodyTrackingBones.LeftToes,
                HumanBodyBones.RightUpperLeg => (int)BodyTrackingBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg => (int)BodyTrackingBones.RightLowerLeg,
                HumanBodyBones.RightFoot     => (int)BodyTrackingBones.RightFoot,
                HumanBodyBones.RightToes     => (int)BodyTrackingBones.RightToes,
                _ => -1,
            };
            return bodyTrackingBoneId;
        }

        public static HumanBodyBones GetHumanBodyBone(int bodyTrackingBoneId)
        {
            var humanBodyBone = bodyTrackingBoneId switch
            {
                (int)BodyTrackingBones.Hips          => HumanBodyBones.Hips,
                (int)BodyTrackingBones.Spine         => HumanBodyBones.Spine,
                (int)BodyTrackingBones.Chest         => HumanBodyBones.Chest,
                (int)BodyTrackingBones.UpperChest    => HumanBodyBones.UpperChest,
                (int)BodyTrackingBones.Neck          => HumanBodyBones.Neck,
                (int)BodyTrackingBones.Head          => HumanBodyBones.Head,
                (int)BodyTrackingBones.LeftShoulder  => HumanBodyBones.LeftShoulder,
                (int)BodyTrackingBones.LeftUpperArm  => HumanBodyBones.LeftUpperArm,
                (int)BodyTrackingBones.LeftLowerArm  => HumanBodyBones.LeftLowerArm,
                (int)BodyTrackingBones.LeftHand      => HumanBodyBones.LeftHand,
                (int)BodyTrackingBones.RightShoulder => HumanBodyBones.RightShoulder,
                (int)BodyTrackingBones.RightUpperArm => HumanBodyBones.RightUpperArm,
                (int)BodyTrackingBones.RightLowerArm => HumanBodyBones.RightLowerArm,
                (int)BodyTrackingBones.RightHand     => HumanBodyBones.RightHand,
                (int)BodyTrackingBones.LeftUpperLeg  => HumanBodyBones.LeftUpperLeg,
                (int)BodyTrackingBones.LeftLowerLeg  => HumanBodyBones.LeftLowerLeg,
                (int)BodyTrackingBones.LeftFoot      => HumanBodyBones.LeftFoot,
                (int)BodyTrackingBones.LeftToes      => HumanBodyBones.LeftToes,
                (int)BodyTrackingBones.RightUpperLeg => HumanBodyBones.RightUpperLeg,
                (int)BodyTrackingBones.RightLowerLeg => HumanBodyBones.RightLowerLeg,
                (int)BodyTrackingBones.RightFoot     => HumanBodyBones.RightFoot,
                (int)BodyTrackingBones.RightToes     => HumanBodyBones.RightToes,
                _ => HumanBodyBones.LastBone,
            };
            return humanBodyBone;
        }
    }
}
