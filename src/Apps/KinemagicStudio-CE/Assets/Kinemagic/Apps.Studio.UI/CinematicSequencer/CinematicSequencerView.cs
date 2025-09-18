using CinematicSequencer;
using CinematicSequencer.UI;
using UnityEngine;

namespace Kinemagic.Apps.Studio.UI.CinematicSequencer
{
    public sealed class CinematicSequencerView : MonoBehaviour
    {
        [SerializeField] private Camera _sceneViewCamera;
        [SerializeField] private TimelineEditorView _sequenceEditorView;
        [SerializeField] private KeyframeEditorView _animationEditorView;
        [SerializeField] private SaveConfirmationDialogView _saveDialogView;

        public void Hide()
        {
            _sceneViewCamera.gameObject.SetActive(false);
            _sequenceEditorView.Hide();
            _animationEditorView.Hide();
            _saveDialogView.Hide();
        }

        public void ShowSequenceEditor()
        {
            SetCameraTarget(_sequenceEditorView.SceneView.RenderTexture);
            _sceneViewCamera.gameObject.SetActive(true);
            _sequenceEditorView.Show();
        }

        public void ShowAnimationEditor()
        {
            SetCameraTarget(_animationEditorView.SceneView.RenderTexture);
            _sceneViewCamera.gameObject.SetActive(true);
            _animationEditorView.Show();
        }

        private void SetCameraTarget(RenderTexture renderTexture)
        {
            _sceneViewCamera.targetTexture = renderTexture;
        }
    }
}
