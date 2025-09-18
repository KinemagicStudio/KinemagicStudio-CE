using System;
using System.Collections.Generic;
using Kinemagic.AppCore.Utils;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using R3;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class MotionDataSourceMonitor : IMotionDataSourceMonitor
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<DataSourceId, IMotionDataSource> _dataSources = new();
        private readonly Dictionary<DataSourceId, ProcessingStatus> _processingStatus = new();
        private readonly ITimeSystem _timeSystem = TimeSystemProvider.GetTimeSystem();
        private readonly float _intervalTimeSeconds = 1.0f;

        private readonly Subject<MotionDataSourceStatus> _onStatusChanged = new();
        public Observable<MotionDataSourceStatus> StatusChanged => _onStatusChanged;

        public MotionDataSourceMonitor()
        {
            Observable.Interval(TimeSpan.FromSeconds(_intervalTimeSeconds))
                .Subscribe(_ => CheckStatus())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _dataSources.Clear();
        }

        public void Register(IMotionDataSource dataSource)
        {
            if (dataSource == null) throw new ArgumentNullException(nameof(dataSource));
            _dataSources.Add(dataSource.Id, dataSource);
        }

        public void Unregister(DataSourceId id)
        {
            _dataSources.Remove(id);
            _processingStatus.Remove(id);
        }

        private void CheckStatus()
        {
            var currentTime = (float)_timeSystem.GetElapsedTime().TotalSeconds;

            foreach (var dataSource in _dataSources.Values)
            {
                var lastUpdatedTime = dataSource.LastUpdatedTime;
                if (lastUpdatedTime <= 0f)
                {
                    UpdateStatus(dataSource, ProcessingStatus.NotStarted);
                }
                else if (lastUpdatedTime == float.MaxValue)
                {
                    UpdateStatus(dataSource, ProcessingStatus.Completed);
                }
                else
                {
                    if (currentTime - lastUpdatedTime < _intervalTimeSeconds)
                    {
                        UpdateStatus(dataSource, ProcessingStatus.InProgress);
                    }
                    else
                    {
                        UpdateStatus(dataSource, ProcessingStatus.Stalled);
                    }
                }
            }
        }

        private void UpdateStatus(IMotionDataSource dataSource, ProcessingStatus currentStatus)
        {
            if (_processingStatus.TryGetValue(dataSource.Id, out var previousStatus) && previousStatus == currentStatus)
            {
                return;
            }
            _processingStatus[dataSource.Id] = currentStatus;
            _onStatusChanged.OnNext(new MotionDataSourceStatus(dataSource.Id, currentStatus));
        }
    }
}
