using System;
using System.Collections.Generic;
using System.Linq;
using Kinemagic.Apps.Studio.Contracts.Motion;
using UnityEngine;
using UniVRM10;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class FaceExpressionHandler
    {
        private readonly Dictionary<FaceExpressionKey, IFaceExpressionTarget> _faceExpressionTargets = new();

        public FaceExpressionHandler(Transform targetTransform)
        {
            var vrmInstance = targetTransform.gameObject.GetComponentInChildren<Vrm10Instance>();
            var skinnedMeshRenderers = targetTransform.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            var faceExpressionKeys = Enum.GetValues(typeof(FaceExpressionKey)).Cast<FaceExpressionKey>();

            foreach (var faceExpressionKey in faceExpressionKeys)
            {
                var faceExpressionKeyName = faceExpressionKey.ToString();

                // FaceBlendShapeTarget
                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    var blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(faceExpressionKeyName);
                    if (blendShapeIndex >= 0)
                    {
                        _faceExpressionTargets[faceExpressionKey] = new FaceBlendShapeTarget(skinnedMeshRenderer, blendShapeIndex);
                    }
                }

                // VrmExpressionTarget
                if (vrmInstance != null && Enum.TryParse(typeof(UniVRM10.ExpressionPreset), faceExpressionKeyName, out var result))
                {
                    var vrmExpressionKey = new UniVRM10.ExpressionKey((UniVRM10.ExpressionPreset)result);
                    _faceExpressionTargets[faceExpressionKey] = new VrmExpressionTarget(vrmInstance.Runtime.Expression, vrmExpressionKey);
                }
            }
        }

        public void SetWeight(FaceExpressionKey faceExpressionKey, float value)
        {
            if (_faceExpressionTargets.TryGetValue(faceExpressionKey, out var faceExpressionTarget))
            {
                faceExpressionTarget.SetWeight(value);
            }
        }
    }
}
