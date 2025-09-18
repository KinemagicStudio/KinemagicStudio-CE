using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class SpatialCoordinateProvider : MonoBehaviour
    {
        [SerializeField] Transform _transform;

        public bool TryGetTransform(out Transform transform)
        {
            if (_transform == null)
            {
                transform = null;
                return false;
            }

            transform = _transform;
            return true;
        }
    }
}
