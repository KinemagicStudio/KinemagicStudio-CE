using System.Collections.Generic;

namespace Kinemagic.Apps.Studio.FeatureCore.MotionDataSource.Infrastructure
{
    public sealed class SequentialIdPool
    {
        private readonly object _lock = new();
        private readonly Queue<int> _releasedIds = new();

        private int _nextId;

        public SequentialIdPool(int firstId = 1)
        {
            _nextId = firstId;
        }

        public int GetNextId(bool reuseReleasedIds = true)
        {
            int value = 0;

            lock (_lock)
            {
                if (!reuseReleasedIds)
                {
                    value = _nextId++;
                }
                else if (!_releasedIds.TryDequeue(out value))
                {
                    value = _nextId++;
                }
            }

            return value;
        }

        public void ReleaseId(int value)
        {
            lock (_lock)
            {
                if (!_releasedIds.Contains(value))
                {
                    _releasedIds.Enqueue(value);
                }
            }
        }
    }
}