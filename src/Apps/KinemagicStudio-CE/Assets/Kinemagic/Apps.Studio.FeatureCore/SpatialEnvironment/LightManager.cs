using System.Collections.Generic;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class LightManager
    {
        private readonly Dictionary<int, Light> _lights = new();

        public bool TryGet(int id, out Light light)
        {
            return _lights.TryGetValue(id, out light);
        }

        public void Add(int id, Light light)
        {
            _lights[id] = light;
        }

        public void Remove(int id)
        {
            _lights.Remove(id);
        }

        public void Clear()
        {
            _lights.Clear();
        }
    }
}
