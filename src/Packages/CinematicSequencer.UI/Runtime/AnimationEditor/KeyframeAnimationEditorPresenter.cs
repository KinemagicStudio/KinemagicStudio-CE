using System;
using System.Linq;
using System.Threading;
using CinematicSequencer.Animation;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Profiler = UnityEngine.Profiling.Profiler;

namespace CinematicSequencer.UI
{
    public sealed class KeyframeAnimationEditorPresenter : IDisposable
    {
        private readonly KeyframeAnimationEditor _animationEditor;
        private readonly KeyframeEditorView _view; // _animationEditorView;
        private readonly SaveConfirmationDialogView _saveConfirmationDialogView;
        private readonly SceneView _sceneView;

        private readonly CancellationTokenSource _cts = new();

        private AnimationFrame _currentFrame = null;
        
        public KeyframeAnimationEditorPresenter(
            KeyframeAnimationEditor keyframeAnimationEditor,
            KeyframeEditorView view)
        {
            _animationEditor = keyframeAnimationEditor;
            _view = view;
            _saveConfirmationDialogView = _view.SaveConfirmationDialogView;
            _sceneView = _view.SceneView;

            _animationEditor.OnLoaded += OnLoaded;
            _animationEditor.OnUnloaded += OnUnloaded;
            _animationEditor.OnAnimationEvaluated += OnEvaluated;

            // Subscribe to view events
            _sceneView.CameraPoseUpdated += OnSceneViewCameraPoseUpdated;
            _view.OnSaveRequested += SaveClipData;
            _view.OnTimeCursorMoved += OnTimeCursorMoved;
            _view.OnPropertyValueChanged += UpdateKeyframeProperty;
            _view.OnAddKeyframeRequested += AddKeyframe;
            _view.OnKeyframeMarkerClicked += SelectKeyframe;
            _view.OnKeyframeDeleteRequested += RemoveKeyframe;
            _view.OnCloseButtonClicked += HandleCloseButtonClicked;
            
            // Add handlers for target ID controls
            // _view.OnTargetIdChanged += HandleTargetIdChanged; // TODO: Delete
            
            // WIP
            _view.OnPlayButtonClicked += () =>
            {
                _animationEditor.Play();
            };
            
            _view.OnPauseButtonClicked += () =>
            {
                _animationEditor.Pause();
            };
            
            _view.OnStopButtonClicked += () =>
            {
                _animationEditor.Stop();
            };
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            _animationEditor.OnLoaded -= OnLoaded;
            _animationEditor.OnUnloaded -= OnUnloaded;
            _animationEditor.OnAnimationEvaluated -= OnEvaluated;

            // Unsubscribe from view events
            _sceneView.CameraPoseUpdated -= OnSceneViewCameraPoseUpdated;
            _view.OnSaveRequested -= SaveClipData;
            _view.OnTimeCursorMoved -= OnTimeCursorMoved;
            _view.OnPropertyValueChanged -= UpdateKeyframeProperty;
            _view.OnAddKeyframeRequested -= AddKeyframe;
            _view.OnKeyframeMarkerClicked -= SelectKeyframe;
            _view.OnKeyframeDeleteRequested -= RemoveKeyframe;
            _view.OnCloseButtonClicked -= HandleCloseButtonClicked;
            
            // Remove handlers for target ID controls
            // _view.OnTargetIdChanged -= HandleTargetIdChanged; // TODO: Delete
        }

        private void SaveClipData()
        {
            _animationEditor.SaveAsync(_cts.Token).Forget();
        }

        private void HandleCloseButtonClicked()
        {
            if (_animationEditor.HasUnsavedChanges)
            {
                // Show save confirmation dialog
                _saveConfirmationDialogView.Show(result =>
                {
                    _saveConfirmationDialogView.Hide();
                    
                    switch (result)
                    {
                        case SaveConfirmationDialogView.DialogResult.Save:
                            // Save and close
                            _animationEditor.SaveAsync(_cts.Token)
                                .ContinueWith(() => _animationEditor.UnloadClipData())
                                .Forget();
                            break;
                            
                        case SaveConfirmationDialogView.DialogResult.DontSave:
                            // Close without saving
                            _animationEditor.UnloadClipData();
                            break;
                            
                        case SaveConfirmationDialogView.DialogResult.Cancel:
                            // Do nothing, stay in editor
                            break;
                    }
                });
            }
            else
            {
                // No unsaved changes, just close
                _animationEditor.UnloadClipData();
            }
        }

        private void OnLoaded()
        {
            _animationEditor.UpdatePreviewTargetId(999);
            _view.Show();
            UpdateUI();
        }

        private void OnUnloaded()
        {
            _view.Hide();
        }

        private void UpdateUI()
        {
            _view.SetTitle(_animationEditor.ClipDataName);
            _view.SetTotalDuration(_animationEditor.ClipDataDuration);
            _view.SetProperties(_animationEditor.GetProperties());
            _view.ClearKeyframeMarkers();

            foreach (var property in _animationEditor.GetProperties())
            {
                foreach (var keyframe in _animationEditor.GetKeyframes(property.Name)) // TODO: Avoid allocation
                {
                    _view.AddKeyframeTimeMarker(keyframe.Time);
                    _view.AddKeyframeMarker(property.Name, keyframe.Time, keyframe.Value);
                }
            }
        }

        // TODO: Delete
        private void HandleTargetIdChanged(int newTargetId)
        {
            Debug.Log($"<color=cyan>[KeyframeAnimationEditorPresenter] Target ID changed to: {newTargetId}</color>");
            _animationEditor.UpdatePreviewTargetId(newTargetId);
        }

        private void OnTimeCursorMoved(float time)
        {
            _animationEditor.Evaluate(time);
        }

        private void OnEvaluated(int targetId, AnimationFrame frame)
        {
            Profiler.BeginSample("KeyframeAnimationEditorPresenter.OnEvaluated");

            _currentFrame = frame;

            _view.UpdateTimeDisplay(frame.Time);
            _view.UpdatePropertyValues(frame.Properties, false);

            if (frame.Type == DataType.CameraPose && targetId == 999)
            {
                var posX = frame.Properties[0].Value;
                var posY = frame.Properties[1].Value;
                var posZ = frame.Properties[2].Value;
                var eulerX = frame.Properties[3].Value;
                var eulerY = frame.Properties[4].Value;
                var eulerZ = frame.Properties[5].Value;

                _sceneView.UpdateCameraTransform(new Vector3(posX, posY, posZ), Quaternion.Euler(eulerX, eulerY, eulerZ));
                // _sceneView.UpdateCameraParameters(fov);
            }

            Profiler.EndSample();
        }

        private void OnSceneViewCameraPoseUpdated((Vector3 Position, Vector3 EulerAngles) pose)
        {
            if (_currentFrame != null && _currentFrame.Type == DataType.CameraPose)
            {
                var eulerAngleX = _currentFrame.GetProperty(3).Value;
                var eulerAngleY = _currentFrame.GetProperty(4).Value;
                var eulerAngleZ = _currentFrame.GetProperty(5).Value;
                var deltaAngleX = Mathf.DeltaAngle(eulerAngleX, pose.EulerAngles.x);
                var deltaAngleY = Mathf.DeltaAngle(eulerAngleY, pose.EulerAngles.y);
                var deltaAngleZ = Mathf.DeltaAngle(eulerAngleZ, pose.EulerAngles.z);

                var newFrame = new AnimationFrame(DataType.CameraPose, 6);
                newFrame.SetTime(_currentFrame.Time);
                newFrame.SetProperty(0, PoseAnimation.PropertyNames.PositionX, pose.Position.x);
                newFrame.SetProperty(1, PoseAnimation.PropertyNames.PositionY, pose.Position.y);
                newFrame.SetProperty(2, PoseAnimation.PropertyNames.PositionZ, pose.Position.z);
                newFrame.SetProperty(3, PoseAnimation.PropertyNames.EulerAngleX, eulerAngleX + deltaAngleX);
                newFrame.SetProperty(4, PoseAnimation.PropertyNames.EulerAngleY, eulerAngleY + deltaAngleY);
                newFrame.SetProperty(5, PoseAnimation.PropertyNames.EulerAngleZ, eulerAngleZ + deltaAngleZ);
                _currentFrame = newFrame;
            }
        }

        private void UpdateKeyframeProperty(KeyframeId keyframeId, object value)
        {
            var floatValue = Convert.ToSingle(value); // Test: int, bool
            _animationEditor.UpdateKeyframeValue(keyframeId.PropertyName, keyframeId.Time, floatValue);
        }

        private void AddKeyframe()
        {
            if (_currentFrame == null) return;

            var time = _currentFrame.Time;
            _view.AddKeyframeTimeMarker(time);

            foreach (var (propertyName, value) in _currentFrame.Properties)
            {
                if (_animationEditor.TryAddKeyframe(propertyName, time, value) >= 0)
                {
                    _view.AddKeyframeMarker(propertyName, time, value);
                }
            }
        }

        private void SelectKeyframe(KeyframeId keyframeId)
        {
            var keyframes = _animationEditor.GetKeyframesAtTime(keyframeId.Time);
            _view.UpdateTimeDisplay(keyframeId.Time);
            _view.UpdatePropertyValues(keyframes, true);
        }

        private void RemoveKeyframe(KeyframeId keyframeId)
        {
            if (keyframeId.PropertyName == "TimeMarker") // TODO: Constant
            {
                foreach (var property in _animationEditor.GetProperties())
                {
                    if (_animationEditor.RemoveKeyframe(property.Name, keyframeId.Time))
                    {
                        _view.RemoveKeyframeMarker(new KeyframeId(property.Name, keyframeId.TimeMs));
                    }
                }
            }
            else
            {
                if (_animationEditor.RemoveKeyframe(keyframeId.PropertyName, keyframeId.Time))
                {
                    _view.RemoveKeyframeMarker(keyframeId);
                }
            }
            
            var count = _animationEditor.GetKeyframesAtTime(keyframeId.Time)
                        .Select(x => x.Keyframe)
                        .Count(keyframe => keyframe != null);
            
            if (count <= 0)
            {
                _view.RemoveKeyframeTimeMarker(keyframeId.Time);
            }
        }
    }
}
