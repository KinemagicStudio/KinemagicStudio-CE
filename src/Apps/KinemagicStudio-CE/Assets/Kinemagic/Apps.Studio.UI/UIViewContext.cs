using R3;

namespace Kinemagic.Apps.Studio.UI
{
    public sealed class UIViewContext
    {
        public readonly ReactiveProperty<UIPageType> CurrentPage = new(UIPageType.Unknown);
    }
}
