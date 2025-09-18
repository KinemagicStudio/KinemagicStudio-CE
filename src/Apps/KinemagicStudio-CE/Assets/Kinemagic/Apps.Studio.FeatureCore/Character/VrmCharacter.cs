using Kinemagic.Apps.Studio.Contracts.Character;
using UniGLTF;
using UnityEngine;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class VrmCharacter
    {
        private readonly RuntimeGltfInstance _gltfInstance;

        public InstanceId InstanceId { get; set; }
        public string Name { get; }
        public Transform RootTransform { get; }
        public Animator Animator { get; }

        public VrmCharacter(RuntimeGltfInstance gltfInstance, string name)
        {
            _gltfInstance = gltfInstance;
            Name = name;
            RootTransform = gltfInstance.Root.transform;
            Animator = gltfInstance.GetComponent<Animator>();
        }

        public void Dispose()
        {
            _gltfInstance.Dispose();
        }

        public void ShowMeshes()
        {
            _gltfInstance.ShowMeshes();
        }

        public void EnableUpdateWhenOffscreen()
        {
            _gltfInstance.EnableUpdateWhenOffscreen();
        }
    }
}
