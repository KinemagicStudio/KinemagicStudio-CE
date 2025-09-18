using System;
using System.Diagnostics;

namespace Kinemagic.AppCore.Utils
{
    public static class TimeSystemProvider
    {
        private static ITimeSystem _timeSystem;

        public static void SetTimeSystem(ITimeSystem timeSystem)
        {
            _timeSystem = timeSystem;
        }

        public static ITimeSystem GetTimeSystem()
        {
            if (_timeSystem == null)
            {
                _timeSystem = new DefaultTimeSystem();
            }
            return _timeSystem;
        }
    }

    public sealed class DefaultTimeSystem : ITimeSystem
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public TimeSpan GetElapsedTime() => _stopwatch.Elapsed;
        public void Reset() => _stopwatch.Restart();
    }
}