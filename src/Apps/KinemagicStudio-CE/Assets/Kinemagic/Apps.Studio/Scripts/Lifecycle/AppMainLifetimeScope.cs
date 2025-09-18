using EngineLooper.VContainer;
using VContainer;
using VContainer.Unity;
using Debug = UnityEngine.Debug;

namespace Kinemagic.Apps.Studio.Lifecycle
{
    public sealed class AppMainLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEngineLooperEntryPoint<AppMain>(Lifetime.Singleton);
            Debug.Log($"<color=cyan>[{nameof(AppMainLifetimeScope)}] Configured</color>");
        }
    }
}
