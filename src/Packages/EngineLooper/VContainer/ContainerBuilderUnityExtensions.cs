#if VCONTAINER
using System;
using EngineLooper.VContainer.Internal;
using VContainer;

namespace EngineLooper.VContainer
{
    public readonly struct EntryPointsBuilder
    {
        public static void EnsureDispatcherRegistered(IContainerBuilder containerBuilder)
        {
            if (!containerBuilder.Exists(typeof(EntryPointDispatcher), false))
            {
                containerBuilder.Register<EntryPointDispatcher>(Lifetime.Scoped);
                containerBuilder.RegisterBuildCallback(container =>
                {
                    container.Resolve<EntryPointDispatcher>().Dispatch();
                });
            }
        }

        readonly IContainerBuilder containerBuilder;
        readonly Lifetime lifetime;

        public EntryPointsBuilder(IContainerBuilder containerBuilder, Lifetime lifetime)
        {
            this.containerBuilder = containerBuilder;
            this.lifetime = lifetime;
        }

        public RegistrationBuilder Add<T>()
            => containerBuilder.Register<T>(lifetime).AsImplementedInterfaces();

        public void OnException(Action<Exception> exceptionHandler)
            => containerBuilder.RegisterEntryPointExceptionHandler(exceptionHandler);
    }

    public static class ContainerBuilderUnityExtensions
    {
        public static RegistrationBuilder RegisterEngineLooperEntryPoint<T>(this IContainerBuilder builder, Lifetime lifetime = Lifetime.Singleton)
        {
            EntryPointsBuilder.EnsureDispatcherRegistered(builder);
            return builder.Register<T>(lifetime).AsImplementedInterfaces();
        }

        public static void RegisterEntryPointExceptionHandler(
            this IContainerBuilder builder,
            Action<Exception> exceptionHandler)
        {
            builder.RegisterInstance(new EntryPointExceptionHandler(exceptionHandler));
        }
    }
}
#endif
