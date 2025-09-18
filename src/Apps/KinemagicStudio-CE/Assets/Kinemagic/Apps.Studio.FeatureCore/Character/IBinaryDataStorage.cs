using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.Character;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public interface IBinaryDataStorage
    {
        UniTask<byte[]> LoadAsync(string key, BinaryDataStorageType storageType, CancellationToken cancellationToken = default);
    }
}
