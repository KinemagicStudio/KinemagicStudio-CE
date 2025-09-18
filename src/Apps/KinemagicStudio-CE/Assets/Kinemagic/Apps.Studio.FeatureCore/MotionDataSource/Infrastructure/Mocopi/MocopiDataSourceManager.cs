#if MOCOPI_RECEIVER_PLUGIN

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using Mocopi.Receiver.Core;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class MocopiDataSourceManager : IMotionDataSourceManager
    {
        private readonly Dictionary<int, MocopiUdpReceiver> _mocopiUdpReceivers = new();
        private readonly SequentialIdPool _dataSourceIdPool;

        public MocopiDataSourceManager(SequentialIdPool dataSourceIdPool)
        {
            _dataSourceIdPool = dataSourceIdPool;
        }

        public void Dispose()
        {
            foreach (var mocopiUdpReceiver in _mocopiUdpReceivers.Values)
            {
                mocopiUdpReceiver.UdpStop();
            }
            _mocopiUdpReceivers.Clear();
        }

        public async UniTask<IMotionDataSource> CreateAsync(MotionDataSourceKey dataSourceKey, MotionDataSourceType dataSourceType)
        {
            if (dataSourceType != MotionDataSourceType.Mocopi)
            {
                throw new ArgumentException($"{nameof(dataSourceType)} must be {nameof(MotionDataSourceType.Mocopi)}.");
            }

            var dataSourceId = _dataSourceIdPool.GetNextId();

            var streamingReceiverId = -1;
            foreach (var registeredData in _mocopiUdpReceivers)
            {
                if (dataSourceKey.Port == registeredData.Value.Port)
                {
                    streamingReceiverId = registeredData.Key;
                    break;
                }
            }

            if (streamingReceiverId < 0)
            {
                _mocopiUdpReceivers.Add(dataSourceId, new MocopiUdpReceiver(dataSourceKey.Port));
            }
            else
            {
                _mocopiUdpReceivers.Add(dataSourceId, _mocopiUdpReceivers[streamingReceiverId]);
            }

            var dataBuffer = new MocopiDataBuffer(dataSourceId);

            var mocopiUdpReceiver = _mocopiUdpReceivers[dataSourceId];
            mocopiUdpReceiver.OnReceiveFrameData += dataBuffer.UpdateSkeleton;
            mocopiUdpReceiver.UdpStart();

            return dataBuffer;
        }
    }
}
#endif
