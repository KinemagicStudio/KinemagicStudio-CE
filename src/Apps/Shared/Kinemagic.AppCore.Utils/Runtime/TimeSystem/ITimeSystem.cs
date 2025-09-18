using System;

namespace Kinemagic.AppCore.Utils
{
    public interface ITimeSystem
    {
        TimeSpan GetElapsedTime();
        void Reset();
    }
}