using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    [CreateAssetMenu(fileName = "GaussianSplatSceneConfig", menuName = "Kinemagic/GaussianSplatSceneConfig")]
    public sealed class GaussianSplatSceneConfig : ScriptableObject
    {
        public Shader SplatsShader;
        public Shader CompositeShader;
        public Shader DebugPointsShader;
        public Shader DebugBoxesShader;
        public ComputeShader SplatUtilitiesComputeShader;
    }
}