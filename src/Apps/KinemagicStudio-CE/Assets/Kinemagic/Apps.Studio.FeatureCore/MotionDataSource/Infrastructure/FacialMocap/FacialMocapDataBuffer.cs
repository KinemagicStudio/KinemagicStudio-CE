#if FACIAL_CAPTURE_SYNC

using System;
using System.Collections.Generic;
using FacialCaptureSync;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.Motion;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class FacialMocapDataBuffer : IFaceTrackingDataSource, IEyeTrackingDataSource
    {
        readonly Dictionary<int, FaceExpressionKey> _keyMap = new();
        readonly ITimeSystem _timeSystem = TimeSystemProvider.GetTimeSystem();
        readonly TimedDataBuffer<FaceTrackingFrame> _faceTrackingDataBuffer;
        readonly TimedDataBuffer<EyeTrackingFrame> _eyeTrackingDataBuffer;

        public DataSourceId Id { get; }
        public float LastUpdatedTime { get; private set; }

        public FacialMocapDataBuffer(int id, int bufferSize = 2)
        {
            Id = new(id);

            _faceTrackingDataBuffer = new TimedDataBuffer<FaceTrackingFrame>(
                new FaceTrackingFrameInterpolator(), capacity: 64, maxDeltaTime: 1f, delayMode: DelayMode.Auto);
            _eyeTrackingDataBuffer = new TimedDataBuffer<EyeTrackingFrame>(
                new EyeTrackingFrameInterpolator(), capacity: 64, maxDeltaTime: 1f, delayMode: DelayMode.Auto);

            foreach (var faceBlendShapeName in Enum.GetValues(typeof(FacialCaptureSync.BlendShapeNameInCamelCase)))
            {
                if (Enum.TryParse(typeof(FaceExpressionKey), faceBlendShapeName.ToString(), out var result))
                {
                    _keyMap.Add((int)faceBlendShapeName, (FaceExpressionKey)result);
                }
            }
        }

        public void Enqueue(FacialCapture facialCapture)
        {
            var time = (float)_timeSystem.GetElapsedTime().TotalSeconds;

            var faceFrame = new FaceTrackingFrame();
            var eyeFrame = new EyeTrackingFrame();

            for (var i = 0; i < facialCapture.BlendShapeValues.Length; i++)
            {
                if (_keyMap.TryGetValue(i, out var faceExpressionKey))
                {
                    faceFrame.SetValue(faceExpressionKey, facialCapture.BlendShapeValues[i]);
                }
            }

            eyeFrame.LeftEyeEulerAngles = facialCapture.GetLeftEyeLocalEulerAngles();
            eyeFrame.RightEyeEulerAngles = facialCapture.GetRightEyeLocalEulerAngles();

            _faceTrackingDataBuffer.Add(time, time, faceFrame);
            _eyeTrackingDataBuffer.Add(time, time, eyeFrame);
            LastUpdatedTime = time;
        }

        public bool TryGetSample(float time, out FaceTrackingFrame value)
        {
            return _faceTrackingDataBuffer.TryGetSample(time, out value);
        }

        public bool TryGetSample(float time, out EyeTrackingFrame value)
        {
            return _eyeTrackingDataBuffer.TryGetSample(time, out value);
        }
    }
}

#endif