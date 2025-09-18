using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kinemagic.Apps.Studio.Contracts.MotionDataSource;
using R3;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource
{
    public sealed class MotionDataSourceRegistry : IMotionDataSourceRegistry
    {
        private readonly Dictionary<MotionDataSourceKey, MotionDataSourceInfo> _dataSources = new();
        
        private readonly List<IBodyTrackingDataSource> _bodyTrackingDataSources = new();
        private readonly List<IFingerTrackingDataSource> _fingerTrackingDataSources = new();
        private readonly List<IFaceTrackingDataSource> _faceTrackingDataSources = new();
        private readonly List<IEyeTrackingDataSource> _eyeTrackingDataSources = new();

        private readonly Subject<MotionDataSourceInfo> _dataSourceAdded = new();
        private readonly Subject<MotionDataSourceInfo> _dataSourceRemoved = new();

        public IReadOnlyList<IBodyTrackingDataSource> BodyTrackingDataSources => _bodyTrackingDataSources;
        public IReadOnlyList<IFingerTrackingDataSource> FingerTrackingDataSources => _fingerTrackingDataSources;
        public IReadOnlyList<IFaceTrackingDataSource> FaceTrackingDataSources => _faceTrackingDataSources;
        public IReadOnlyList<IEyeTrackingDataSource> EyeTrackingDataSources => _eyeTrackingDataSources;

        public Observable<MotionDataSourceInfo> Added => _dataSourceAdded;
        public Observable<MotionDataSourceInfo> Removed => _dataSourceRemoved;

        // TODO: Preserve for IL2CPP
        public MotionDataSourceRegistry()
        {
        }

        public void Dispose()
        {
            _dataSourceAdded.Dispose();
            _dataSourceRemoved.Dispose();

            _dataSources.Clear();

            _bodyTrackingDataSources.Clear();
            _fingerTrackingDataSources.Clear();
            _faceTrackingDataSources.Clear();
            _eyeTrackingDataSources.Clear();
        }

        public bool Contains(MotionDataSourceKey dataSourceKey)
        {
            return _dataSources.ContainsKey(dataSourceKey);
        }

        public bool TryAdd(MotionDataSourceInfo dataSourceInfo, IMotionDataSource dataSource)
        {
            if (dataSource == null)
            {
                return false;
            }

            if (!_dataSources.TryAdd(dataSourceInfo.Key, dataSourceInfo))
            {
                return false;
            }

            // Body tracking data source
            if (dataSource is IBodyTrackingDataSource bodyTrackingDataSource)
            {
                _bodyTrackingDataSources.Add(bodyTrackingDataSource);
            }
            else
            {
                _bodyTrackingDataSources.Add(null); // TODO: Add comment
            }

            // Finger tracking data source
            if (dataSource is IFingerTrackingDataSource fingerTrackingDataSource)
            {
                _fingerTrackingDataSources.Add(fingerTrackingDataSource);
            }
            else
            {
                _fingerTrackingDataSources.Add(null); // TODO: Add comment
            }

            // Face tracking data source
            if (dataSource is IFaceTrackingDataSource faceTrackingDataSource)
            {
                _faceTrackingDataSources.Add(faceTrackingDataSource);
            }
            else
            {
                _faceTrackingDataSources.Add(null); // TODO: Add comment
            }

            // Eye tracking data source
            if (dataSource is IEyeTrackingDataSource eyeTrackingDataSource)
            {
                _eyeTrackingDataSources.Add(eyeTrackingDataSource);
            }
            else
            {
                _eyeTrackingDataSources.Add(null); // TODO: Add comment
            }

            _dataSourceAdded.OnNext(dataSourceInfo);
            return true;
        }

        public void Remove(int dataSourceId)
        {
            throw new NotImplementedException("Remove method is not implemented yet.");
        }
    }
}
