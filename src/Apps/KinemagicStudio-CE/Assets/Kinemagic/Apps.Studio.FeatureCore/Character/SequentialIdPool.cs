using System.Collections.Generic;

namespace Kinemagic.Apps.Studio.FeatureCore.Character
{
    public sealed class SequentialIdPool
    {
        private readonly object _lock = new();
        private readonly Queue<uint> _releasedIds = new();

        private uint _nextId;

        public SequentialIdPool(uint firstId = 1)
        {
            _nextId = firstId;
        }

        public uint GetNextId(bool reuseReleasedIds = true)
        {
            uint value = 0;

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

        public void ReleaseId(uint value)
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