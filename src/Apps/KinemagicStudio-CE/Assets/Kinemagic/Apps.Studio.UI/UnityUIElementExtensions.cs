using R3;

namespace Kinemagic.Apps.Studio.UI
{
    public static class UnityUIElementExtensions
    {
        public static Observable<Unit> OnClickAsObservable(this UnityEngine.UIElements.Button button)
        {
            return Observable.FromEvent(h => button.clicked += h, h => button.clicked -= h);
        }
    }
}
