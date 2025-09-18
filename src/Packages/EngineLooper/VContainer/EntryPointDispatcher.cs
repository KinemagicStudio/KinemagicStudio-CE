#if VCONTAINER
using System;
using System.Collections.Generic;
using EngineLooper.VContainer.Internal;
using VContainer;
using VContainer.Internal;

namespace EngineLooper.VContainer
{
    public sealed class EntryPointDispatcher : IDisposable
    {
        readonly IObjectResolver container;
        readonly CompositeDisposable disposable = new CompositeDisposable();

        [Inject]
        public EntryPointDispatcher(IObjectResolver container)
        {
            this.container = container;
        }

        public void Dispatch()
        {
            PlayerLoopHelper.EnsureInitialized();

            EntryPointExceptionHandler exceptionHandler = null;
            try
            {
                exceptionHandler = container.Resolve<EntryPointExceptionHandler>();
            }
            catch (VContainerException ex) when (ex.InvalidType == typeof(EntryPointExceptionHandler))
            {
            }

            var initializables = container.Resolve<ContainerLocal<IReadOnlyList<IInitializable>>>().Value;
            for (var i = 0; i < initializables.Count; i++)
            {
                try
                {
                    initializables[i].Initialize();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                        exceptionHandler.Publish(ex);
                    else
                        UnityEngine.Debug.LogException(ex);
                }
            }

            var startables = container.Resolve<ContainerLocal<IReadOnlyList<IStartable>>>().Value;
            if (startables.Count > 0)
            {
                var loopItem = new StartableLoopItem(startables, exceptionHandler);
                disposable.Add(loopItem);
                PlayerLoopHelper.Dispatch(PlayerLoopTiming.Startup, loopItem);
            }

            var tickables = container.Resolve<ContainerLocal<IReadOnlyList<ITickable>>>().Value;
            if (tickables.Count > 0)
            {
                var loopItem = new TickableLoopItem(tickables, exceptionHandler);
                disposable.Add(loopItem);
                PlayerLoopHelper.Dispatch(PlayerLoopTiming.Update, loopItem);
            }

            var lateTickables = container.Resolve<ContainerLocal<IReadOnlyList<ILateTickable>>>().Value;
            if (lateTickables.Count > 0)
            {
                var loopItem = new LateTickableLoopItem(lateTickables, exceptionHandler);
                disposable.Add(loopItem);
                PlayerLoopHelper.Dispatch(PlayerLoopTiming.LateUpdate, loopItem);
            }
        }

        public void Dispose() => disposable.Dispose();
    }
}
#endif
