using System;
using Kinemagic.AppCore.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using uOSC;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class VMCProtocolStreamingReceiver : IDisposable
    {
        // The specification of the root transform data transmission is different for each application.
        public enum RootTransformType
        {
            TypeA = 0, // Hips position and Hips rotation
            TypeB = 1, // Root position and Hips rotation
            TypeC = 2, // Root position and Root rotation
            TypeD = 3, // Hips position and Root rotation
        }

        public sealed class SkeletonData
        {
            public float Time = 0f;
            public string Name = "Skeleton";
            public Vector3 Scale = Vector3.one;

            /// <summary>
            /// World space position of the root bone.
            /// </summary>
            public Vector3 RootBonePosition = Vector3.zero;

            /// <summary>
            /// World space rotation of the root bone.
            /// </summary>
            public Quaternion RootBoneRotation = Quaternion.identity;

            /// <summary>
            /// Local space rotations of humanoid bones (including the root bone).
            /// </summary>
            public Vector3[] BonePositions = new Vector3[HumanoidBoneCount];

            /// <summary>
            /// Local space rotations of humanoid bones (including the root bone).
            /// </summary>
            public Quaternion[] BoneRotations = new Quaternion[HumanoidBoneCount];
        }

        private const string OscAddress_Time = "/VMC/Ext/T";
        private const string OscAddress_RootTransform = "/VMC/Ext/Root/Pos";
        private const string OscAddress_BoneTransform = "/VMC/Ext/Bone/Pos";

        private static readonly int HumanoidBoneCount = (int)HumanBodyBones.LastBone;

        private readonly ITimeSystem _timeSystem = TimeSystemProvider.GetTimeSystem();
        private readonly float _maxDeltaTime = 1f;

        private readonly SkeletonData[] _skeletonDataBuffer;
        private readonly int _bufferSize = 2;
        private readonly long _bufferMask;

        private long _bufferHead = 0;
        private long _bufferTail = 0;
        private long _enqueueIndex = 0;

        private OscServer _oscServer;
        private bool _isRunning;
        private int _port;
        private RootTransformType _rootTransformType;

        // NOTE:
        // 1. If the time data cannot be received from an external application, 
        //    the frame time is updated when the Hips data is received.
        // 2. If the time and the Hips data cannot be received from an external application, 
        //    the frame time is updated when the Spine data is received.
        private long _lastEnqueuedIndexOfHipsBone = -1;
        private long _lastEnqueuedIndexOfSpineBone = -1;
        private float _lastFrameEnqueuedTime = float.MaxValue;

        public event Action<SkeletonData> OnDataReceived;

        public bool IsRunning => _isRunning;
        public int Port => _port;

        public VMCProtocolStreamingReceiver(int port, RootTransformType rootTransformType)
        {
            Assert.IsTrue(IsPowerOfTwo(_bufferSize), "The buffer size must be a power of two.");
            _bufferMask = _bufferSize - 1;

            _port = port;
            _rootTransformType = rootTransformType;

            _skeletonDataBuffer = new SkeletonData[_bufferSize];
            for (var i = 0; i < _bufferSize; i++)
            {
                _skeletonDataBuffer[i] = new SkeletonData();
            }

            _oscServer = new OscServer(port, targetFrameRate: 60, isBackground: true);
            _oscServer.OnDataReceived.AddListener(OnOscDataReceived);
        }

        public void Dispose()
        {
            Stop();
            _oscServer.Dispose();
            _oscServer = null;
        }

        public void Start()
        {
            _oscServer.Start();
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
            _oscServer.Stop();
        }

        private void OnOscDataReceived(Message message)
        {
            if (message.address == OscAddress_Time)
            {
                OnTimeReceived(message);
            }
            else if (message.address == OscAddress_RootTransform)
            {
                OnRootTransformReceived(message);
            }
            else if (message.address == OscAddress_BoneTransform)
            {
                OnBoneTransformReceived(message);
            }
        }

        private void OnTimeReceived(Message data)
        {
            var elementCount = data.values.Length;

            // [VMC Protocol]
            // /VMC/Ext/T (float){time}
            if (elementCount == 1)
            {
                OnTimeReceivedInternal((float)data.values[0]);
            }
        }

        private void OnTimeReceivedInternal(float time)
        {
            _skeletonDataBuffer[_enqueueIndex].Time = time;

            
            // Update the enqueue position to insert the next data.
            _bufferTail++;
            _enqueueIndex = _bufferTail & _bufferMask;

            // When the buffer overflows, the head data (oldest data) is deleted.
            var bufferFreeCount = _bufferSize - (int) (_bufferTail - _bufferHead);
            if (bufferFreeCount <= 0)
            {
                _bufferHead++;
            }

            //
            // NOTE:
            // Try to get the data at the head in the buffer.
            // The data is not deleted from the buffer in this method.
            // When the buffer overflows during enqueue, the head data (oldest data) is automatically deleted.
            //
            var bufferIndex = _bufferHead & _bufferMask;
            OnDataReceived?.Invoke(_skeletonDataBuffer[bufferIndex]);
        }

        private void OnRootTransformReceived(Message data)
        {
            var elementCount = data.values.Length;

            // [VMC Protocol] (V2.0 or later)
            // /VMC/Ext/Root/Pos (string){name} (float){p.x} (float){p.y} (float){p.z} (float){q.x} (float){q.y} (float){q.z} (float){q.w}
            if (elementCount >= 8)
            {
                var name = (string)data.values[0];
                var px = (float)data.values[1];
                var py = (float)data.values[2];
                var pz = (float)data.values[3];
                var qx = (float)data.values[4];
                var qy = (float)data.values[5];
                var qz = (float)data.values[6];
                var qw = (float)data.values[7];

                if (_rootTransformType == RootTransformType.TypeC)
                {
                    _skeletonDataBuffer[_enqueueIndex].RootBonePosition = new Vector3(px, py, pz);
                    _skeletonDataBuffer[_enqueueIndex].RootBoneRotation = new Quaternion(qx, qy, qz, qw);
                }
                else if (_rootTransformType == RootTransformType.TypeB)
                {
                    _skeletonDataBuffer[_enqueueIndex].RootBonePosition = new Vector3(px, py, pz);
                }
            }
            // [VMC Protocol] (V2.1 or later)
            // /VMC/Ext/Root/Pos (string){name} (float){p.x} (float){p.y} (float){p.z} (float){q.x} (float){q.y} (float){q.z} (float){q.w} 
            //                   (float){s.x} (float){s.y} (float){s.z} (float){o.x} (float){o.y} (float){o.z}
            if (elementCount >= 14)
            {
                var sx = (float)data.values[8];
                var sy = (float)data.values[9];
                var sz = (float)data.values[10];
                var ox = (float)data.values[11];
                var oy = (float)data.values[12];
                var oz = (float)data.values[13];

                _skeletonDataBuffer[_enqueueIndex].Scale = new Vector3(sx, sy, sz);
            }
        }

        private void OnBoneTransformReceived(Message data)
        {
            var elementCount = data.values.Length;

            // [VMC Protocol]
            // /VMC/Ext/Bone/Pos (string){name} (float){p.x} (float){p.y} (float){p.z} (float){q.x} (float){q.y} (float){q.z} (float){q.w}
            if (elementCount == 8)
            {
                var name = (string)data.values[0];
                var px = (float)data.values[1];
                var py = (float)data.values[2];
                var pz = (float)data.values[3];
                var qx = (float)data.values[4];
                var qy = (float)data.values[5];
                var qz = (float)data.values[6];
                var qw = (float)data.values[7];

                var boneId = GetHumanBodyBoneId(name);

                if (boneId == (int)HumanBodyBones.Hips)
                {
                    ProcessFrameTimeIfNeeded(boneId, _enqueueIndex);                
                    _lastEnqueuedIndexOfHipsBone = _enqueueIndex;
                }
                else if (boneId == (int)HumanBodyBones.Spine)
                {
                    ProcessFrameTimeIfNeeded(boneId, _enqueueIndex);
                    _lastEnqueuedIndexOfSpineBone = _enqueueIndex;
                }

                if (boneId == (int)HumanBodyBones.Hips)
                {
                    if (_rootTransformType == RootTransformType.TypeA)
                    {
                        _skeletonDataBuffer[_enqueueIndex].RootBonePosition = new Vector3(px, py, pz);
                        _skeletonDataBuffer[_enqueueIndex].RootBoneRotation = new Quaternion(qx, qy, qz, qw);
                    }
                    else if (_rootTransformType == RootTransformType.TypeB)
                    {
                        _skeletonDataBuffer[_enqueueIndex].RootBoneRotation = new Quaternion(qx, qy, qz, qw);
                    }
                }

                if (boneId >= 0)
                {
                    _skeletonDataBuffer[_enqueueIndex].BonePositions[boneId] = new Vector3(px, py, pz);
                    _skeletonDataBuffer[_enqueueIndex].BoneRotations[boneId] = new Quaternion(qx, qy, qz, qw);
                }
            }
        }

        private void ProcessFrameTimeIfNeeded(int boneId, long nextEnqueueIndex)
        {
            var currentTime = _timeSystem.GetElapsedTime().TotalSeconds;
            if (currentTime - _lastFrameEnqueuedTime > _maxDeltaTime)
            {
                // Reset
                _lastEnqueuedIndexOfHipsBone = -1;
                _lastEnqueuedIndexOfSpineBone = -1;
                _lastFrameEnqueuedTime = float.MaxValue;
            }

            //
            // NOTE:
            // 1. If the time data cannot be received from an external application, 
            //    the frame time is updated when the Hips data is received.
            // 2. If the time and the Hips data cannot be received from an external application, 
            //    the frame time is updated when the Spine data is received.
            //
            if (boneId == (int)HumanBodyBones.Hips &&
                _lastEnqueuedIndexOfHipsBone == nextEnqueueIndex)
            {
                _lastFrameEnqueuedTime = (float)currentTime;
                OnTimeReceivedInternal(_lastFrameEnqueuedTime);
            }
            else if (boneId == (int)HumanBodyBones.Spine &&
                _lastEnqueuedIndexOfSpineBone == nextEnqueueIndex &&
                _lastEnqueuedIndexOfHipsBone != nextEnqueueIndex)
            {
                _lastFrameEnqueuedTime = (float)currentTime;
                OnTimeReceivedInternal(_lastFrameEnqueuedTime);
            }
        }

        private static bool IsPowerOfTwo(int value)
        {
            // If the value is a power of two, the result of bitwise AND operation with (value - 1) is zero.
            return (value > 1) && (value & (value - 1)) is 0;
        }

        public static int GetHumanBodyBoneId(string boneName)
        {
            var humanBodyBoneId = boneName switch
            {
                // Body
                "Hips"                    => (int)HumanBodyBones.Hips,
                "Spine"                   => (int)HumanBodyBones.Spine,
                "Chest"                   => (int)HumanBodyBones.Chest,
                "UpperChest"              => (int)HumanBodyBones.UpperChest,
                "Neck"                    => (int)HumanBodyBones.Neck,
                "Head"                    => (int)HumanBodyBones.Head,
                "LeftShoulder"            => (int)HumanBodyBones.LeftShoulder,
                "LeftUpperArm"            => (int)HumanBodyBones.LeftUpperArm,
                "LeftLowerArm"            => (int)HumanBodyBones.LeftLowerArm,
                "LeftHand"                => (int)HumanBodyBones.LeftHand,
                "RightShoulder"           => (int)HumanBodyBones.RightShoulder,
                "RightUpperArm"           => (int)HumanBodyBones.RightUpperArm,
                "RightLowerArm"           => (int)HumanBodyBones.RightLowerArm, 
                "RightHand"               => (int)HumanBodyBones.RightHand,
                "LeftUpperLeg"            => (int)HumanBodyBones.LeftUpperLeg,
                "LeftLowerLeg"            => (int)HumanBodyBones.LeftLowerLeg,
                "LeftFoot"                => (int)HumanBodyBones.LeftFoot,
                "LeftToes"                => (int)HumanBodyBones.LeftToes,
                "RightUpperLeg"           => (int)HumanBodyBones.RightUpperLeg,
                "RightLowerLeg"           => (int)HumanBodyBones.RightLowerLeg,
                "RightFoot"               => (int)HumanBodyBones.RightFoot,
                "RightToes"               => (int)HumanBodyBones.RightToes,
                // Left hand
                "LeftThumbProximal"       => (int)HumanBodyBones.LeftThumbProximal,
                "LeftThumbIntermediate"   => (int)HumanBodyBones.LeftThumbIntermediate,
                "LeftThumbDistal"         => (int)HumanBodyBones.LeftThumbDistal,
                "LeftIndexProximal"       => (int)HumanBodyBones.LeftIndexProximal,
                "LeftIndexIntermediate"   => (int)HumanBodyBones.LeftIndexIntermediate,
                "LeftIndexDistal"         => (int)HumanBodyBones.LeftIndexDistal,
                "LeftMiddleProximal"      => (int)HumanBodyBones.LeftMiddleProximal,
                "LeftMiddleIntermediate"  => (int)HumanBodyBones.LeftMiddleIntermediate,
                "LeftMiddleDistal"        => (int)HumanBodyBones.LeftMiddleDistal,
                "LeftRingProximal"        => (int)HumanBodyBones.LeftRingProximal,
                "LeftRingIntermediate"    => (int)HumanBodyBones.LeftRingIntermediate,
                "LeftRingDistal"          => (int)HumanBodyBones.LeftRingDistal,
                "LeftLittleProximal"      => (int)HumanBodyBones.LeftLittleProximal,
                "LeftLittleIntermediate"  => (int)HumanBodyBones.LeftLittleIntermediate,
                "LeftLittleDistal"        => (int)HumanBodyBones.LeftLittleDistal,
                // Right hand
                "RightThumbProximal"       => (int)HumanBodyBones.RightThumbProximal,
                "RightThumbIntermediate"   => (int)HumanBodyBones.RightThumbIntermediate,
                "RightThumbDistal"         => (int)HumanBodyBones.RightThumbDistal,
                "RightIndexProximal"       => (int)HumanBodyBones.RightIndexProximal,
                "RightIndexIntermediate"   => (int)HumanBodyBones.RightIndexIntermediate,
                "RightIndexDistal"         => (int)HumanBodyBones.RightIndexDistal,
                "RightMiddleProximal"      => (int)HumanBodyBones.RightMiddleProximal,
                "RightMiddleIntermediate"  => (int)HumanBodyBones.RightMiddleIntermediate,
                "RightMiddleDistal"        => (int)HumanBodyBones.RightMiddleDistal,
                "RightRingProximal"        => (int)HumanBodyBones.RightRingProximal,
                "RightRingIntermediate"    => (int)HumanBodyBones.RightRingIntermediate,
                "RightRingDistal"          => (int)HumanBodyBones.RightRingDistal,
                "RightLittleProximal"      => (int)HumanBodyBones.RightLittleProximal,
                "RightLittleIntermediate"  => (int)HumanBodyBones.RightLittleIntermediate,
                "RightLittleDistal"        => (int)HumanBodyBones.RightLittleDistal,
                _ => -1,
            };
            return humanBodyBoneId;
        }
    }
}
