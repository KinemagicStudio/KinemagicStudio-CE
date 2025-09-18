using System;

namespace Kinemagic.AppCore.Utils
{
    public sealed class TimedDataBuffer<T>
    {
        private readonly RingBuffer<(float Time, float SourceTime, T Value)> _buffer;
        private readonly IInterpolator<T> _interpolator;
        private readonly float _maxDeltaTime;

        private float _timeReferencePoint;
        private float _sourceTimeReferencePoint;

        private float _autoDelayTime;
        private float _fixedDelayTime;

        public int Capacity => _buffer.Capacity;
        public int Count => _buffer.Count;

        public float TimeReferencePoint => _timeReferencePoint;
        public float SourceTimeReferencePoint => _sourceTimeReferencePoint;

        public DelayMode DelayMode { get; set; } = DelayMode.Auto;
        public float AutoDelayTime => _autoDelayTime;
        public float FixedDelayTime
        {
            get => _fixedDelayTime;
            set
            {
                if (value < 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Delay time cannot be negative.");
                }
                _fixedDelayTime = value;
            }
        }

        public (float Time, float SourceTime, T Value) this[int index] => _buffer[index];

        public TimedDataBuffer(IInterpolator<T> interpolator, int capacity = 4, float maxDeltaTime = 1f, DelayMode delayMode = DelayMode.Auto)
        {
            _buffer = new RingBuffer<(float Time, float SourceTime, T Value)>(capacity);
            _interpolator = interpolator;
            _maxDeltaTime = maxDeltaTime;
            DelayMode = delayMode;
        }

        public void Clear()
        {
            _buffer.Clear();
            _timeReferencePoint = 0f;
            _sourceTimeReferencePoint = 0f;
            _autoDelayTime = 0f;
        }

        public void Add(float time, float sourceTime, T value)
        {
            var lastEnqueuedTime = _buffer.Count > 0 ? _buffer.PeekTail().Time : 0f;
            if (time - lastEnqueuedTime > _maxDeltaTime)
            {
                Clear();
            }

            if (_buffer.Count == 0)
            {
                _timeReferencePoint = time;
                _sourceTimeReferencePoint = sourceTime;
                _buffer.Push((time, sourceTime, value));
                return;
            }

            for (var indexFromEnd = 0; indexFromEnd < _buffer.Count; indexFromEnd++)
            {
                var index = (_buffer.Count - 1) - indexFromEnd;
                ref readonly var data = ref _buffer.Peek(index);

                _autoDelayTime = Math.Max(_autoDelayTime, (time - data.Time) + (sourceTime - data.SourceTime));

                if (data.Time < time)
                {
                    _buffer.InsertFromTail(indexFromEnd, (time, sourceTime, value));
                    return;
                }

                if (data.Time == time)
                {
                    _buffer[index] = (time, sourceTime, value);
                    return;
                }
            }

            _buffer.Push((time, sourceTime, value));
        }

        public bool TryGetSample(float time, out T value)
        {
            value = default;

            if (_buffer.Count == 0)
            {
                return false;
            }

            var delayTime = DelayMode switch
            {
                DelayMode.None => 0f,
                DelayMode.Auto => _autoDelayTime,
                DelayMode.Constant => _fixedDelayTime,
                _ => throw new ArgumentOutOfRangeException(nameof(DelayMode), "Invalid delay mode.")
            };

            for (var index = 1; index < _buffer.Count; index++)
            {
                ref readonly var next = ref _buffer.Peek(index);
                ref readonly var prev = ref _buffer.Peek(index - 1);

                var timestamp = time - _timeReferencePoint - delayTime + _sourceTimeReferencePoint;
                var t = (timestamp - prev.SourceTime) / (next.SourceTime - prev.SourceTime);

                if (0f <= t && t <= 1f)
                {
                    value = _interpolator.Interpolate(prev.Value, next.Value, t);
                    return true;
                }
            }

            return false;
        }
    }

    public enum DelayMode
    {
        None,
        Auto,
        Constant,
    }
}