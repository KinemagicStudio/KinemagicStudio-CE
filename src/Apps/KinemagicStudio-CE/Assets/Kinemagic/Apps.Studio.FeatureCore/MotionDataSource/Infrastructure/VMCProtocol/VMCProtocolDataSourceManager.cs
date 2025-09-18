using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class VMCProtocolDataSourceManager : IMotionDataSourceManager
    {
        private readonly Dictionary<int, VMCProtocolStreamingReceiver> _streamingReceivers = new();
        private readonly SequentialIdPool _dataSourceIdPool;

        public VMCProtocolDataSourceManager(SequentialIdPool dataSourceIdPool)
        {
            _dataSourceIdPool = dataSourceIdPool;
        }

        public void Dispose()
        {
            foreach (var streamingReceiver in _streamingReceivers.Values)
            {
                streamingReceiver.Dispose();
            }
            _streamingReceivers.Clear();
        }

        public async UniTask<IMotionDataSource> CreateAsync(MotionDataSourceKey dataSourceKey, MotionDataSourceType dataSourceType)
        {
            if (dataSourceType != MotionDataSourceType.VMCProtocolTypeA &&
                dataSourceType != MotionDataSourceType.VMCProtocolTypeB)
            {
                throw new ArgumentException($"{nameof(dataSourceType)} must be " +
                                            $"{nameof(MotionDataSourceType.VMCProtocolTypeA)} or {nameof(MotionDataSourceType.VMCProtocolTypeB)}.");
            }

            var dataSourceId = _dataSourceIdPool.GetNextId();

            var streamingReceiverId = -1;
            foreach (var registeredData in _streamingReceivers)
            {
                if (dataSourceKey.Port == registeredData.Value.Port)
                {
                    streamingReceiverId = registeredData.Key;
                    break;
                }
            }

            if (streamingReceiverId < 0)
            {
                var rootTransformType = dataSourceType switch
                {
                    MotionDataSourceType.VMCProtocolTypeA => VMCProtocolStreamingReceiver.RootTransformType.TypeA,
                    MotionDataSourceType.VMCProtocolTypeB => VMCProtocolStreamingReceiver.RootTransformType.TypeB,
                    _ => throw new ArgumentException($"{nameof(dataSourceType)} must be " +
                                                     $"{nameof(MotionDataSourceType.VMCProtocolTypeA)} or {nameof(MotionDataSourceType.VMCProtocolTypeB)}.")
                };
                _streamingReceivers.Add(dataSourceId, new VMCProtocolStreamingReceiver(dataSourceKey.Port, rootTransformType));
            }
            else
            {
                _streamingReceivers.Add(dataSourceId, _streamingReceivers[streamingReceiverId]);
            }

            var dataBuffer = new VMCProtocolDataBuffer(dataSourceId);
            _streamingReceivers[dataSourceId].OnDataReceived += dataBuffer.Enqueue;
            _streamingReceivers[dataSourceId].Start();

            return dataBuffer;
        }
    }
}
