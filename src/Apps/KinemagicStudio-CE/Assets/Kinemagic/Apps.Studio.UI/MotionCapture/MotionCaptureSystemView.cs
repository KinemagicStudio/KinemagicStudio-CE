using RuntimeNodeGraph.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kinemagic.Apps.Studio.UI.MotionCapture
{
    public sealed class MotionCaptureSystemView : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;

        public void Show()
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
