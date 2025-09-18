using System;
using System.Collections.Generic;
using System.Threading;
using EngineLooper.VContainer.Internal;

namespace EngineLooper.VContainer
{
    sealed class StartableLoopItem : IPlayerLoopItem, IDisposable
    {
        readonly IEnumerable<IStartable> entries;
        readonly EntryPointExceptionHandler exceptionHandler;
        bool disposed;

        public StartableLoopItem(
            IEnumerable<IStartable> entries,
            EntryPointExceptionHandler exceptionHandler)
        {
            this.entries = entries;
            this.exceptionHandler = exceptionHandler;
        }
    
        public bool MoveNext()
        {
            if (disposed) return false;
            foreach (var x in entries)
            {
                try
                {
                    x.Start();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler == null) throw;
                    exceptionHandler.Publish(ex);
                }
            }
            return false;
        }
    
        public void Dispose() => disposed = true;
    }

    sealed class TickableLoopItem : IPlayerLoopItem, IDisposable
    {
        readonly IReadOnlyList<ITickable> entries;
        readonly EntryPointExceptionHandler exceptionHandler;
        bool disposed;

        public TickableLoopItem(
            IReadOnlyList<ITickable> entries,
            EntryPointExceptionHandler exceptionHandler)
        {
            this.entries = entries;
            this.exceptionHandler = exceptionHandler;
        }

        public bool MoveNext()
        {
            if (disposed) return false;
            for (var i = 0; i < entries.Count; i++)
            {
                try
                {
                    entries[i].Tick();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler == null) throw;
                    exceptionHandler.Publish(ex);
                }
            }
            return !disposed;
        }

        public void Dispose() => disposed = true;
    }

    sealed class LateTickableLoopItem : IPlayerLoopItem, IDisposable
    {
        readonly IReadOnlyList<ILateTickable> entries;
        readonly EntryPointExceptionHandler exceptionHandler;
        bool disposed;

        public LateTickableLoopItem(
            IReadOnlyList<ILateTickable> entries,
            EntryPointExceptionHandler exceptionHandler)
        {
            this.entries = entries;
            this.exceptionHandler = exceptionHandler;
        }

        public bool MoveNext()
        {
            if (disposed) return false;
            for (var i = 0; i < entries.Count; i++)
            {
                try
                {
                    entries[i].LateTick();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler == null) throw;
                    exceptionHandler.Publish(ex);
                }
            }
            return !disposed;
        }

        public void Dispose() => disposed = true;
    }
}