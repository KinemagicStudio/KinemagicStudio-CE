using UnityEngine;

namespace Kinemagic.AppCore.Utils
{
    public sealed class QuaternionInterpolator : IInterpolator<Quaternion>
    {
        public Quaternion Interpolate(Quaternion startValue, Quaternion endValue, float t)
        {
            return Quaternion.Slerp(startValue, endValue, t);
        }
    }
}