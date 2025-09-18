using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public sealed class SpatialEnvironmentScene : IDisposable
    {
        private readonly List<Light> _lights = new();

        public GameObject Root { get; private set; }
        public IReadOnlyList<Light> Lights => _lights;

        /// <summary>
        /// Only available if the built-in Animation module is enabled.
        /// </summary>
        public Animation LegacyAnimation { get; private set; }

        public SpatialEnvironmentScene(GameObject root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public void Dispose()
        {
            foreach (var light in _lights)
            {
                UnityEngine.Object.Destroy(light.gameObject);
            }
            _lights.Clear();

            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
            }
        }

        public void AddLight(Light light)
        {
            if (light != null && !_lights.Contains(light))
            {
                _lights.Add(light);
            }
        }

        public void SetLegacyAnimation(Animation animation)
        {
            LegacyAnimation = animation;
        }

        public static SpatialEnvironmentScene CreateDefault()
        {
            var directionalLight = new GameObject("DirectionalLight").AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.color = Color.white;
            directionalLight.intensity = 1.0f;
            directionalLight.shadows = LightShadows.None;
            directionalLight.transform.position = new Vector3(0, 3, 0);
            directionalLight.transform.rotation = Quaternion.Euler(50, -30, 0);

            var root = new GameObject("DefaultEnvironment");
            directionalLight.transform.SetParent(root.transform, worldPositionStays: true);

            var scene = new SpatialEnvironmentScene(root);
            scene.AddLight(directionalLight);
            return scene;
        }
    }
}
