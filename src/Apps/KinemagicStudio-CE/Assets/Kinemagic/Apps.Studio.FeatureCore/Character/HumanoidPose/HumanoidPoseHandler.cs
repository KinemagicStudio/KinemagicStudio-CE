using System;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class HumanoidPoseHandler : IDisposable
    {
        public static readonly int RootBoneId = (int)BodyTrackingBones.Hips;
        public static readonly int BodyBoneCount = Enum.GetValues(typeof(BodyTrackingBones)).Length;
        public static readonly int FingerBoneCount = Enum.GetValues(typeof(FingerTrackingBones)).Length;

        private readonly HumanPoseHandler _humanPoseHandler;
        private readonly Transform[] _bodyBones = new Transform[BodyBoneCount];
        private readonly Transform[] _fingerBones = new Transform[FingerBoneCount];

        private Vector3 _bodyTrackingRootBonePositionOffset = Vector3.zero;
        private Quaternion _bodyTrackingRootBoneRotationOffset = Quaternion.identity;

        public bool RootBoneOffsetEnabled { get; set; }
        public Vector3 RootBonePosition => _bodyBones[RootBoneId].localPosition;
        public Quaternion RootBoneRotation => _bodyBones[RootBoneId].localRotation;

        public HumanoidPoseHandler(VrmCharacter character)
        {
            _humanPoseHandler = new HumanPoseHandler(character.Animator.avatar, character.RootTransform);

            var rootBoneTransform = character.Animator.GetBoneTransform(HumanBodyBones.Hips);
            SetRootBoneOffset(rootBoneTransform.localPosition, rootBoneTransform.localRotation);

            for (var i = 0; i < BodyBoneCount; i++)
            {
                var humanBodyBone = BodyTrackingHelper.GetHumanBodyBone(i);
                _bodyBones[i] = character.Animator.GetBoneTransform(humanBodyBone);
            }

            for (var i = 0; i < FingerBoneCount; i++)
            {                
                var humanBodyBone = FingerTrackingHelper.GetHumanBodyBone(i);
                _fingerBones[i] = character.Animator.GetBoneTransform(humanBodyBone);
            }
        }

        public void Dispose()
        {
        }

        public void GetHumanPose(ref HumanPose humanPose)
        {
            _humanPoseHandler.GetHumanPose(ref humanPose);
        }

        public void SetHumanPose(ref HumanPose humanPose)
        {
            _humanPoseHandler.SetHumanPose(ref humanPose);
        }

        public void SetRootBoneOffset(Vector3 position, Quaternion rotation)
        {
            _bodyTrackingRootBonePositionOffset = position;
            _bodyTrackingRootBoneRotationOffset = rotation;
        }

        public void SetRootBonePosition(Vector3 position)
        {
            if (RootBoneOffsetEnabled)
            {
                _bodyBones[RootBoneId].localPosition = _bodyTrackingRootBonePositionOffset + position;
            }
            else
            {
                _bodyBones[RootBoneId].localPosition = position;
            }
        }

        public void SetRootBoneRotation(Quaternion rotation)
        {
            if (RootBoneOffsetEnabled)
            {
                _bodyBones[RootBoneId].localRotation = _bodyTrackingRootBoneRotationOffset * rotation;
            }
            else
            {
                _bodyBones[RootBoneId].localRotation = rotation;
            }
        }

        public void SetBodyTrackingPose(BodyTrackingFrame frame)
        {
            for (var boneId = 0; boneId < BodyBoneCount; boneId++)
            {
                _bodyBones[boneId].localRotation = frame.BoneRotations[boneId].ToUnityQuaternion();
            }

            if (RootBoneOffsetEnabled)
            {
                _bodyBones[RootBoneId].localPosition = _bodyTrackingRootBonePositionOffset + frame.RootPosition.ToUnityVector3();
                _bodyBones[RootBoneId].localRotation = _bodyTrackingRootBoneRotationOffset * frame.RootRotation.ToUnityQuaternion();
            }
            else
            {
                _bodyBones[RootBoneId].localPosition = frame.RootPosition.ToUnityVector3();
                _bodyBones[RootBoneId].localRotation = frame.RootRotation.ToUnityQuaternion();
            }
        }

        public void SetFingerTrackingPose(FingerTrackingFrame frame)
        {
            for (var boneId = 0; boneId < FingerBoneCount; boneId++)
            {
                if (boneId != (int)FingerTrackingBones.LeftHand && boneId != (int)FingerTrackingBones.RightHand)
                {
                    _fingerBones[boneId].localRotation = frame.BoneRotations[boneId].ToUnityQuaternion();
                }
            }
        }
    }
}