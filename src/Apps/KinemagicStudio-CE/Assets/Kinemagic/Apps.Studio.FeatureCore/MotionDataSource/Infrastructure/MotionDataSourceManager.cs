using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class MotionDataSourceManager: IMotionDataSourceManager
    {
        private readonly List<IMotionDataSourceManager> _dataSourceManagers = new();

        private VMCProtocolDataSourceManager _vmcProtocolDataSourceManager;
#if FACIAL_CAPTURE_SYNC
        private FacialMocapDataSourceManager _facialMocapDataSourceManager;
#endif
#if MOCOPI_RECEIVER_PLUGIN
        private MocopiDataSourceManager _mocopiDataSourceManager;
#endif

        public MotionDataSourceManager
        (
            VMCProtocolDataSourceManager vmcProtocolDataSourceManager
#if FACIAL_CAPTURE_SYNC
            ,FacialMocapDataSourceManager facialMocapDataSourceManager = null
#endif
#if MOCOPI_RECEIVER_PLUGIN
            ,MocopiDataSourceManager mocopiDataSourceManager = null
#endif
        )
        {
            _vmcProtocolDataSourceManager = vmcProtocolDataSourceManager;
            _dataSourceManagers.Add(vmcProtocolDataSourceManager);

#if FACIAL_CAPTURE_SYNC
            _facialMocapDataSourceManager = facialMocapDataSourceManager;
            _dataSourceManagers.Add(facialMocapDataSourceManager);
#endif
#if MOCOPI_RECEIVER_PLUGIN
            _mocopiDataSourceManager = mocopiDataSourceManager;
            _dataSourceManagers.Add(mocopiDataSourceManager);
#endif
        }

        public void Dispose()
        {
            _vmcProtocolDataSourceManager = null;
#if FACIAL_CAPTURE_SYNC
            _facialMocapDataSourceManager = null;
#endif
#if MOCOPI_RECEIVER_PLUGIN
            _mocopiDataSourceManager = null;
#endif
            foreach (var dataSourceManager in _dataSourceManagers)
            {
                dataSourceManager.Dispose();
            }
            _dataSourceManagers.Clear();
        }

        public async UniTask<IMotionDataSource> CreateAsync(MotionDataSourceKey dataSourceKey, MotionDataSourceType dataSourceType)
        {
            return dataSourceType switch
            {
                MotionDataSourceType.VMCProtocolTypeA => await _vmcProtocolDataSourceManager.CreateAsync(dataSourceKey, dataSourceType),
                MotionDataSourceType.VMCProtocolTypeB => await _vmcProtocolDataSourceManager.CreateAsync(dataSourceKey, dataSourceType),
#if FACIAL_CAPTURE_SYNC
                MotionDataSourceType.iFacialMocap => await _facialMocapDataSourceManager.CreateAsync(dataSourceKey, dataSourceType),
                MotionDataSourceType.FaceMotion3d => await _facialMocapDataSourceManager.CreateAsync(dataSourceKey, dataSourceType),
#endif
#if MOCOPI_RECEIVER_PLUGIN
                MotionDataSourceType.Mocopi => await _mocopiDataSourceManager.CreateAsync(dataSourceKey, dataSourceType),
#endif
                _ => throw new ArgumentException($"Invalid DataSourceType: {dataSourceType}"),
            };
        }
    }
}
