#if FACIAL_CAPTURE_SYNC

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FacialCaptureSync;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class FacialMocapDataSourceManager : IMotionDataSourceManager
    {
        private readonly Dictionary<int, FacialCaptureReceiver> _facialCaptureReceivers = new();
        private readonly SequentialIdPool _dataSourceIdPool;

        public FacialMocapDataSourceManager(SequentialIdPool dataSourceIdPool)
        {
            _dataSourceIdPool = dataSourceIdPool;
        }

        public void Dispose()
        {
            foreach (var facialCaptureReceiver in _facialCaptureReceivers.Values)
            {
                facialCaptureReceiver.Stop();
            }
            _facialCaptureReceivers.Clear();
        }

        public async UniTask<IMotionDataSource> CreateAsync(MotionDataSourceKey dataSourceKey, MotionDataSourceType dataSourceType)
        {
            if (dataSourceType != MotionDataSourceType.iFacialMocap
            && dataSourceType != MotionDataSourceType.FaceMotion3d)
            {
                throw new ArgumentException($"{nameof(dataSourceType)} must be {nameof(MotionDataSourceType.iFacialMocap)} or {nameof(MotionDataSourceType.FaceMotion3d)}.");
            }

            var dataSourceId = _dataSourceIdPool.GetNextId();

            var streamingReceiverId = -1;
            foreach (var registeredReceiver in _facialCaptureReceivers)
            {
                if (dataSourceKey.Port == registeredReceiver.Value.Port)
                {
                    streamingReceiverId = registeredReceiver.Key;
                    break;
                }
            }

            if (streamingReceiverId < 0)
            {
                IFacialCaptureSource facialCaptureDataSource = dataSourceType switch
                {
                    MotionDataSourceType.FaceMotion3d => new Facemotion3d(dataSourceKey.Port),
                    MotionDataSourceType.iFacialMocap => new iFacialMocap(useIndirectConnection: false, dataSourceKey.Port),
                    _ => new iFacialMocap()
                };
                _facialCaptureReceivers.Add(dataSourceId, new FacialCaptureReceiver(facialCaptureDataSource));
            }
            else
            {
                _facialCaptureReceivers.Add(dataSourceId, _facialCaptureReceivers[streamingReceiverId]);
            }

            var dataBuffer = new FacialMocapDataBuffer(dataSourceId);
            _facialCaptureReceivers[dataSourceId].FlipHorizontal = true;
            _facialCaptureReceivers[dataSourceId].OnDataReceived += dataBuffer.Enqueue;
            _facialCaptureReceivers[dataSourceId].Start();

            return dataBuffer;
        }
    }
}

#endif