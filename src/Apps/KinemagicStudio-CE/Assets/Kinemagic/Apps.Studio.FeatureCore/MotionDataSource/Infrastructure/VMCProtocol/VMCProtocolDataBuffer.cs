using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class VMCProtocolDataBuffer : IBodyTrackingDataSource, IFingerTrackingDataSource
    {
        private readonly ITimeSystem _timeSystem = TimeSystemProvider.GetTimeSystem();
        private readonly TimedDataBuffer<BodyTrackingFrame> _bodyTrackingDataBuffer;
        private readonly TimedDataBuffer<FingerTrackingFrame> _fingerTrackingDataBuffer;

        public DataSourceId Id { get; }
        public float LastUpdatedTime { get; private set; }

        public VMCProtocolDataBuffer(int id)
        {
            Id = new(id);

            _bodyTrackingDataBuffer = new TimedDataBuffer<BodyTrackingFrame>(
                new BodyTrackingFrameInterpolator(), capacity: 64, maxDeltaTime: 1f, delayMode: DelayMode.Auto);
            _fingerTrackingDataBuffer = new TimedDataBuffer<FingerTrackingFrame>(
                new FingerTrackingFrameInterpolator(), capacity: 64, maxDeltaTime: 1f, delayMode: DelayMode.Auto);
        }

        public bool TryGetSample(float time, out BodyTrackingFrame value)
        {
            return _bodyTrackingDataBuffer.TryGetSample(time, out value);
        }

        public bool TryGetSample(float time, out FingerTrackingFrame value)
        {
            return _fingerTrackingDataBuffer.TryGetSample(time, out value);
        }

        public void Enqueue(VMCProtocolStreamingReceiver.SkeletonData skeletonData)
        {
            var time = (float)_timeSystem.GetElapsedTime().TotalSeconds;

            var bodyFrame = new BodyTrackingFrame();
            var fingerFrame = new FingerTrackingFrame();

            bodyFrame.RootPosition = skeletonData.RootBonePosition.ToSystemNumericsVector3();
            bodyFrame.RootRotation = skeletonData.RootBoneRotation.ToSystemNumericsQuaternion();
            bodyFrame.Scale = skeletonData.Scale.ToSystemNumericsVector3();

            var boneCount = skeletonData.BoneRotations.Length;
            for (var i = 0; i < boneCount; i++)
            {
                var bodyBoneId = GetBodyTrackingBoneId(i);
                var fingerBoneId = GetFingerTrackingBoneId(i);

                if (bodyBoneId < 0 && fingerBoneId < 0) continue;

                if (bodyBoneId >= 0)
                {
                    bodyFrame.BoneRotations[bodyBoneId] = skeletonData.BoneRotations[i].ToSystemNumericsQuaternion();
                }

                if (fingerBoneId >= 0)
                {
                    fingerFrame.BoneRotations[fingerBoneId] = skeletonData.BoneRotations[i].ToSystemNumericsQuaternion();
                }
            }

            _bodyTrackingDataBuffer.Add(time, skeletonData.Time, bodyFrame);
            _fingerTrackingDataBuffer.Add(time, skeletonData.Time, fingerFrame);
            LastUpdatedTime = time;
        }

        public static int GetBodyTrackingBoneId(int humanBodyBoneId)
        {
            var bodyTrackingBoneId = humanBodyBoneId switch
            {
                (int)HumanBodyBones.Hips => (int)BodyTrackingBones.Hips,
                (int)HumanBodyBones.Spine => (int)BodyTrackingBones.Spine,
                (int)HumanBodyBones.Chest => (int)BodyTrackingBones.Chest,
                (int)HumanBodyBones.UpperChest => (int)BodyTrackingBones.UpperChest,
                (int)HumanBodyBones.Neck => (int)BodyTrackingBones.Neck,
                (int)HumanBodyBones.Head => (int)BodyTrackingBones.Head,
                (int)HumanBodyBones.LeftShoulder => (int)BodyTrackingBones.LeftShoulder,
                (int)HumanBodyBones.LeftUpperArm => (int)BodyTrackingBones.LeftUpperArm,
                (int)HumanBodyBones.LeftLowerArm => (int)BodyTrackingBones.LeftLowerArm,
                (int)HumanBodyBones.LeftHand => (int)BodyTrackingBones.LeftHand,
                (int)HumanBodyBones.RightShoulder => (int)BodyTrackingBones.RightShoulder,
                (int)HumanBodyBones.RightUpperArm => (int)BodyTrackingBones.RightUpperArm,
                (int)HumanBodyBones.RightLowerArm => (int)BodyTrackingBones.RightLowerArm,
                (int)HumanBodyBones.RightHand => (int)BodyTrackingBones.RightHand,
                (int)HumanBodyBones.LeftUpperLeg => (int)BodyTrackingBones.LeftUpperLeg,
                (int)HumanBodyBones.LeftLowerLeg => (int)BodyTrackingBones.LeftLowerLeg,
                (int)HumanBodyBones.LeftFoot => (int)BodyTrackingBones.LeftFoot,
                (int)HumanBodyBones.LeftToes => (int)BodyTrackingBones.LeftToes,
                (int)HumanBodyBones.RightUpperLeg => (int)BodyTrackingBones.RightUpperLeg,
                (int)HumanBodyBones.RightLowerLeg => (int)BodyTrackingBones.RightLowerLeg,
                (int)HumanBodyBones.RightFoot => (int)BodyTrackingBones.RightFoot,
                (int)HumanBodyBones.RightToes => (int)BodyTrackingBones.RightToes,
                _ => -1,
            };
            return bodyTrackingBoneId;
        }

        public static int GetFingerTrackingBoneId(int humanBodyBoneId)
        {
            var fingerTrackingBoneId = humanBodyBoneId switch
            {
                // Left hand
                (int)HumanBodyBones.LeftHand => (int)FingerTrackingBones.LeftHand,
                (int)HumanBodyBones.LeftThumbProximal => (int)FingerTrackingBones.LeftThumbProximal,
                (int)HumanBodyBones.LeftThumbIntermediate => (int)FingerTrackingBones.LeftThumbIntermediate,
                (int)HumanBodyBones.LeftThumbDistal => (int)FingerTrackingBones.LeftThumbDistal,
                (int)HumanBodyBones.LeftIndexProximal => (int)FingerTrackingBones.LeftIndexProximal,
                (int)HumanBodyBones.LeftIndexIntermediate => (int)FingerTrackingBones.LeftIndexIntermediate,
                (int)HumanBodyBones.LeftIndexDistal => (int)FingerTrackingBones.LeftIndexDistal,
                (int)HumanBodyBones.LeftMiddleProximal => (int)FingerTrackingBones.LeftMiddleProximal,
                (int)HumanBodyBones.LeftMiddleIntermediate => (int)FingerTrackingBones.LeftMiddleIntermediate,
                (int)HumanBodyBones.LeftMiddleDistal => (int)FingerTrackingBones.LeftMiddleDistal,
                (int)HumanBodyBones.LeftRingProximal => (int)FingerTrackingBones.LeftRingProximal,
                (int)HumanBodyBones.LeftRingIntermediate => (int)FingerTrackingBones.LeftRingIntermediate,
                (int)HumanBodyBones.LeftRingDistal => (int)FingerTrackingBones.LeftRingDistal,
                (int)HumanBodyBones.LeftLittleProximal => (int)FingerTrackingBones.LeftLittleProximal,
                (int)HumanBodyBones.LeftLittleIntermediate => (int)FingerTrackingBones.LeftLittleIntermediate,
                (int)HumanBodyBones.LeftLittleDistal => (int)FingerTrackingBones.LeftLittleDistal,
                // Right hand
                (int)HumanBodyBones.RightHand => (int)FingerTrackingBones.RightHand,
                (int)HumanBodyBones.RightThumbProximal => (int)FingerTrackingBones.RightThumbProximal,
                (int)HumanBodyBones.RightThumbIntermediate => (int)FingerTrackingBones.RightThumbIntermediate,
                (int)HumanBodyBones.RightThumbDistal => (int)FingerTrackingBones.RightThumbDistal,
                (int)HumanBodyBones.RightIndexProximal => (int)FingerTrackingBones.RightIndexProximal,
                (int)HumanBodyBones.RightIndexIntermediate => (int)FingerTrackingBones.RightIndexIntermediate,
                (int)HumanBodyBones.RightIndexDistal => (int)FingerTrackingBones.RightIndexDistal,
                (int)HumanBodyBones.RightMiddleProximal => (int)FingerTrackingBones.RightMiddleProximal,
                (int)HumanBodyBones.RightMiddleIntermediate => (int)FingerTrackingBones.RightMiddleIntermediate,
                (int)HumanBodyBones.RightMiddleDistal => (int)FingerTrackingBones.RightMiddleDistal,
                (int)HumanBodyBones.RightRingProximal => (int)FingerTrackingBones.RightRingProximal,
                (int)HumanBodyBones.RightRingIntermediate => (int)FingerTrackingBones.RightRingIntermediate,
                (int)HumanBodyBones.RightRingDistal => (int)FingerTrackingBones.RightRingDistal,
                (int)HumanBodyBones.RightLittleProximal => (int)FingerTrackingBones.RightLittleProximal,
                (int)HumanBodyBones.RightLittleIntermediate => (int)FingerTrackingBones.RightLittleIntermediate,
                (int)HumanBodyBones.RightLittleDistal => (int)FingerTrackingBones.RightLittleDistal,
                // Others
                _ => -1,
            };
            return fingerTrackingBoneId;
        }
    }
}
