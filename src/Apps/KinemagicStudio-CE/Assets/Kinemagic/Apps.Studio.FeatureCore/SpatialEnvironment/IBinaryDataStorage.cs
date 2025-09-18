using System.Threading;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.SpatialEnvironment;

namespace Kinemagic.Apps.Studio.FeatureCore.SpatialEnvironment
{
    public interface IBinaryDataStorage
    {
        UniTask<byte[]> LoadAsync(string key, BinaryDataStorageType storageType, CancellationToken cancellationToken = default);
    }
}
