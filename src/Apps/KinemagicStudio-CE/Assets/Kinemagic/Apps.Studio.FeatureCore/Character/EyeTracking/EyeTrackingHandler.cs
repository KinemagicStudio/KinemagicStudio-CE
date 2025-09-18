using UnityEngine;
using UniVRM10;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class EyeTrackingHandler
    {
        private readonly Vrm10RuntimeLookAt _vrmLookAt;

        public EyeTrackingHandler(Transform targetTransform)
        {
            var vrmInstance = targetTransform.gameObject.GetComponentInChildren<Vrm10Instance>();
            if (vrmInstance != null)
            {
                _vrmLookAt = vrmInstance.Runtime.LookAt;
            }
        }

        public void UpdateEyeDirection(System.Numerics.Vector3 leftEyeEulerAngles, System.Numerics.Vector3 rightEyeEulerAngles)
        {
            if (_vrmLookAt != null)
            {
                var yaw = (leftEyeEulerAngles.Y + rightEyeEulerAngles.Y) / 2f;
                var pitch = (leftEyeEulerAngles.X + rightEyeEulerAngles.X) / 2f;
                _vrmLookAt.LookAtInput = new LookAtInput()
                {
                    // NOTE: The pitch value is inverted because of the vrm-1.0 specification.
                    YawPitch = new LookAtEyeDirection(yaw, -pitch)
                };
            }
        }
    }
}
