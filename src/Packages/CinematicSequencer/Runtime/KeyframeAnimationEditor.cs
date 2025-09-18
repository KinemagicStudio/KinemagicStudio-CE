using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CinematicSequencer.Animation;
using CinematicSequencer.IO;

// namespace CinematicSequencer.Animation
namespace CinematicSequencer
{
    // public sealed class AnimationClipEditor : IDisposable
    public sealed class KeyframeAnimationEditor : IDisposable
    {
        private readonly IClipDataRepository _clipDataRepository;
        private readonly TimelinePlayer _sequencePlayer;

        private Timeline _cachedSequence; // The sequence which was used before keyframe animation editing.
        private TimelineClip _timelineClip;
        private IAnimationClipData _clipData;
        private bool _hasUnsavedChanges = false;
        private bool _isActive = false;

        public string ClipDataName => _clipData?.Name;
        public DataType dataDataType => _clipData?.Type ?? DataType.Unknown;
        public float ClipDataDuration => _clipData?.GetDuration() ?? 0f;
        public bool HasUnsavedChanges => _hasUnsavedChanges;
        public bool IsActive => _isActive;

        public event Action OnLoaded;
        public event Action OnUnloaded;
        public event Action OnSaved; // 保存完了イベントを追加
        public event Action<int, AnimationFrame> OnAnimationEvaluated;

        public KeyframeAnimationEditor(IClipDataRepository clipDataRepository, TimelinePlayer sequencePlayer)
        {
            _clipDataRepository = clipDataRepository;
            _sequencePlayer = sequencePlayer;
            _sequencePlayer.OnAnimationEvaluate += OnEvaluated;
        }

        public void Dispose()
        {
            _sequencePlayer.OnAnimationEvaluate -= OnEvaluated;
        }

        public async UniTask SaveAsync(CancellationToken cancellationToken)
        {
            if (_clipData == null)
            {
                throw new InvalidOperationException("Clip data is null.");
            }

            await _clipDataRepository.SaveAsync(_clipData.Id.ToString(), _clipData, cancellationToken);
            _hasUnsavedChanges = false;
            OnSaved?.Invoke(); // 保存完了を通知
        }

        public async UniTask LoadAsync(string clipId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clipId))
            {
                throw new ArgumentException("Clip ID cannot be null or empty.", nameof(clipId));
            }

            // TODO: Test
            _clipData = await _clipDataRepository.LoadAsync(clipId, cancellationToken) as IAnimationClipData;

            if (_clipData == null)
            {
                throw new InvalidOperationException($"Failed to load clip data with ID: {clipId}");
            }

            // Cache the sequence which was used before keyframe animation editing.
            _cachedSequence = _sequencePlayer.Sequence;

            // Create a temporary sequence.
            var temporarySequence = new Timeline($"TemporarySequence");
            var timelineTrack = temporarySequence.CreateTrack("TemporaryTrack", _clipData.Type);
            _timelineClip = temporarySequence.AddClip(timelineTrack.Id, 0, _clipData);
            _sequencePlayer.Sequence = temporarySequence;

            _isActive = true;
            OnLoaded?.Invoke();
        }

        public void CreateNewClipData(DataType type)
        {
            switch (type)
            {
                case DataType.CameraPose:
                    _clipData = new PoseAnimation(DataType.CameraPose);
                    break;
                case DataType.LightPose:
                    _clipData = new PoseAnimation(DataType.LightPose);
                    break;
                case DataType.LightProperties:
                    _clipData = new LightPropertiesAnimation();
                    break;
                default:
                    throw new NotSupportedException($"Clip type {type} is not supported.");
            }

            _hasUnsavedChanges = true;

            // Cache the sequence which was used before keyframe animation editing.
            _cachedSequence = _sequencePlayer.Sequence;

            // Create a temporary sequence.
            var temporarySequence = new Timeline($"TemporarySequence");
            var timelineTrack = temporarySequence.CreateTrack("TemporaryTrack", _clipData.Type);
            _timelineClip = temporarySequence.AddClip(timelineTrack.Id, 0, _clipData);
            _sequencePlayer.Sequence = temporarySequence;

            _isActive = true;
            OnLoaded?.Invoke();
        }

        public void UnloadClipData()
        {
            _sequencePlayer.Sequence = _cachedSequence;
            _cachedSequence = null;

            _timelineClip = null;
            _clipData = null;
            _hasUnsavedChanges = false;

            _isActive = false;
            OnUnloaded?.Invoke();
        }

        public void UpdateClipDataName(string value)
        {
            if (_clipData != null && _clipData.Name != value)
            {
                _clipData.Name = value;
                _hasUnsavedChanges = true;
            }
        }

        public IReadOnlyList<(string PropertyName, Keyframe? Keyframe)> GetKeyframesAtTime(float time)
        {
            return _clipData.GetKeyframes(time);
        }
        
        public IReadOnlyList<Keyframe> GetKeyframes(string propertyName)
        {
            return _clipData.GetKeyframes(propertyName);
        }

        public int TryAddKeyframe(string propertyName, float time, float value)
        {
            int result = _clipData.TryAddKeyframe(propertyName, new Keyframe(time, value));
            if (result >= 0)
            {
                _hasUnsavedChanges = true;
            }
            return result;
        }

        public bool RemoveKeyframe(string propertyName, float time)
        {
            if (_clipData.RemoveKeyframe(propertyName, time))
            {
                _hasUnsavedChanges = true;
                return true;
            }
            return false;
        }

        public void UpdateKeyframeValue(string propertyName, float time, float value)
        {
            if (_clipData.TryGetKeyframe(propertyName, time, out var existingKeyframe))
            {
                if (existingKeyframe.Value != value)
                {
                    var updateResult = _clipData.UpdateKeyframeValue(propertyName, time, value);
                    if (updateResult)
                    {
                        _hasUnsavedChanges = true;
                    }
                }
            }
            else
            {
                UnityEngine.Debug.Log($"<color=orange>[UpdateKeyframeValue] Keyframe not found at time {time}.</color>");
            }
        }

        public AnimationPropertyInfo[] GetProperties()
        {
            return _clipData.GetProperties();
        }

        public void Play() => _sequencePlayer.Play();
        public void Pause() => _sequencePlayer.Pause();
        public void Stop() => _sequencePlayer.Stop();

        public void UpdatePreviewTargetId(int value)
        {
            _timelineClip.TargetId = value;
        }

        public void Evaluate(float time)
        {
            _sequencePlayer.SetTime(time);
        }

        private void OnEvaluated(int previewTargetId, AnimationFrame frame)
        {
            OnAnimationEvaluated?.Invoke(previewTargetId, frame);
        }
    }
}
