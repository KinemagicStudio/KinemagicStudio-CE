using UnityEngine;
using UniVRM10;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class VrmExpressionTarget : IFaceExpressionTarget
    {
        private readonly Vrm10RuntimeExpression _vrmRuntimeExpression;
        private readonly ExpressionKey _vrmExpressionKey;

        public VrmExpressionTarget(Vrm10RuntimeExpression vrmRuntimeExpression, ExpressionKey vrmExpressionKey)
        {
            _vrmRuntimeExpression = vrmRuntimeExpression;
            _vrmExpressionKey = vrmExpressionKey;
        }

        public void SetWeight(float value)
        {
            _vrmRuntimeExpression.SetWeight(_vrmExpressionKey, value);
        }
    }
}
