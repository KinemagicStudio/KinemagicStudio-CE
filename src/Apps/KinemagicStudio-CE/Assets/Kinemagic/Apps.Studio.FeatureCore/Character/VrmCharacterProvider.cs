using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.Character;
using UniGLTF;
using UniVRM10;
using UniVRM10.Extensions.Materials.lilToon;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class VrmCharacterProvider
    {
        private readonly IBinaryDataStorage _binaryDataStorage;

        public VrmCharacterProvider(IBinaryDataStorage binaryDataStorage)
        {
            _binaryDataStorage = binaryDataStorage;
        }

        public async UniTask<VrmCharacter> LoadAsync(string key, BinaryDataStorageType storageType, CancellationToken cancellationToken = default)
        {
            var bytes = await _binaryDataStorage.LoadAsync(key, storageType, cancellationToken);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            var materialGenerator = new lilToonMaterialDescriptorGenerator();

            var loadedVrm = await Vrm10.LoadBytesAsync(bytes,
                canLoadVrm0X: true,
                showMeshes: false,
                materialGenerator: materialGenerator,
                ct: cancellationToken);

            if (loadedVrm == null)
            {
                return null;
            }

            // loadedVrm.gameObject.AddComponent<SkinnedMeshColliderInitializer>(); // TODO

            var vrmCharacter = new VrmCharacter(loadedVrm.GetComponent<RuntimeGltfInstance>(), loadedVrm.Vrm.Meta.Name);
            vrmCharacter.ShowMeshes();
            vrmCharacter.EnableUpdateWhenOffscreen();

            await UniTask.DelayFrame(1, cancellationToken: cancellationToken); // NOTE: Wait for ControlRig to be applied.

            return vrmCharacter;
        }
    }
}
