using UnityEngine;

namespace Kinemagic.AppCore.Utils
{
    public sealed class Vector3Interpolator : IInterpolator<Vector3>
    {
        public Vector3 Interpolate(Vector3 startValue, Vector3 endValue, float t)
        {
            return Vector3.Lerp(startValue, endValue, t);
        }
    }
}