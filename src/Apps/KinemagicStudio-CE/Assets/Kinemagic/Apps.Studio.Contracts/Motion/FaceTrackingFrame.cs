using System;

namespace Kinemagic.Apps.Studio.Contracts.Motion
{
    public sealed class FaceTrackingFrame
    {
        private readonly int _faceExpressionKeyCount;
        private readonly float[] _faceExpressionValues;

        public FaceTrackingFrame()
        {
            _faceExpressionKeyCount = Enum.GetValues(typeof(FaceExpressionKey)).Length;
            _faceExpressionValues = new float[_faceExpressionKeyCount];
            Clear();
        }

        public void Clear()
        {
            for (var i = 0; i < _faceExpressionKeyCount; i++)
            {
                _faceExpressionValues[i] = float.MinValue;
            }
        }

        public void SetValue(FaceExpressionKey faceExpressionKey, float value)
        {
            _faceExpressionValues[(int)faceExpressionKey] = value;
        }

        public bool TryGetValue(FaceExpressionKey faceExpressionKey, out float value)
        {
            value = _faceExpressionValues[(int)faceExpressionKey];
            return value != float.MinValue;
        }
    }
}
