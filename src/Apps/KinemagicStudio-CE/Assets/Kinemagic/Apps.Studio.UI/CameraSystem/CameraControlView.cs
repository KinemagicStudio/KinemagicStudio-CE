using R3;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.CameraSystem
{
    public sealed class CameraControlView : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        public void Construct(UIViewContext context)
        {
            context.CurrentPage
                .Skip(1)
                .Subscribe(pageType =>
                {
                    if (pageType == UIPageType.CameraControl)
                    {
                        Show();
                    }
                    else
                    {
                        Hide();
                    }
                })
                .AddTo(this);

            Debug.Log($"[{nameof(CameraControlView)}] Constructed");
        }

        private void Show()
        {
            _document.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            _document.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
