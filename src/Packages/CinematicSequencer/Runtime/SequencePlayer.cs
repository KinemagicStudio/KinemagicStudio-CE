using System;
using CinematicSequencer.Animation;
using Profiler = UnityEngine.Profiling.Profiler;

namespace CinematicSequencer
{
    /// <summary>
    /// タイムラインを再生するクラス
    /// </summary>
    public sealed class TimelinePlayer // CinematicSequencePlayer
    {
        private Timeline _sequence;
        private float _currentTime = 0f;
        private float _playbackSpeed = 1f;
        
        public bool IsPlaying { get; private set; }
        public bool IsLooping { get; set; }
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Math.Max(0.01f, value);
        }
        
        public Timeline Sequence
        {
            get => _sequence;
            set
            {
                _sequence = value;
                _currentTime = 0f;
                IsPlaying = false;
            }
        }
                
        public event Action<int, AnimationFrame> OnAnimationEvaluate;
        public event Action<float> OnTimeUpdate;
        public event Action OnTimelineStart;
        public event Action OnTimelinePause;
        public event Action OnTimelineStop;
        public event Action OnTimelineComplete;
        
        public void Play()
        {
            if (_sequence == null) return;
            IsPlaying = true;
            OnTimelineStart?.Invoke();
        }
        
        public void Pause()
        {
            IsPlaying = false;
            OnTimelinePause?.Invoke();
        }
        
        public void Stop()
        {
            IsPlaying = false;
            _currentTime = 0f;
            OnTimelineStop?.Invoke();
        }
        
        public void SetTime(float time)
        {
            if (_sequence == null) return;
            _currentTime = Math.Clamp(time, 0f, _sequence.GetDuration());
            OnTimeUpdate?.Invoke(_currentTime);
            EvaluateTimeline(_currentTime);
        }
        
        /// <summary>
        /// Frequently called method
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsPlaying || _sequence == null) return;

            Profiler.BeginSample("CinematicSequencePlayer.Update");
            
            _currentTime += deltaTime * _playbackSpeed;
            
            var duration = _sequence.GetDuration();
            if (_currentTime > duration)
            {
                if (IsLooping)
                {
                    _currentTime %= duration;
                }
                else
                {
                    _currentTime = duration;
                    IsPlaying = false;
                    OnTimelineComplete?.Invoke();
                }
            }
            
            Profiler.BeginSample("CinematicSequencePlayer.OnTimeUpdate");
            OnTimeUpdate?.Invoke(_currentTime);
            Profiler.EndSample();
            
            EvaluateTimeline(_currentTime);
            
            Profiler.EndSample();
        }
        
        /// <summary>
        /// Frequently called method
        /// </summary>
        private void EvaluateTimeline(float time)
        {
            if (_sequence == null) return;

            Profiler.BeginSample("CinematicSequencePlayer.EvaluateTimeline");

            foreach (var timelineClip in _sequence.GetActiveClipsAtTime(time))
            {
                var localTime = timelineClip.GetLocalTime(time);
                if (localTime < 0) continue;

                if (timelineClip.ClipData is not IAnimationClipData animationClipData) continue;

                var animationFrame = animationClipData.Evaluate(localTime);

                Profiler.BeginSample("CinematicSequencePlayer.OnAnimationEvaluate");
                OnAnimationEvaluate?.Invoke(timelineClip.TargetId, animationFrame);
                Profiler.EndSample();
            }

            Profiler.EndSample();
        }
    }
}