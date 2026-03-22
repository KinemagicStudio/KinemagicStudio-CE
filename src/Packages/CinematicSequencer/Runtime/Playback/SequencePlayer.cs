using System;

namespace CinematicSequencer.Playback
{
    /// <summary>
    /// シーケンスの再生エンジン。
    /// シングルトンを廃止し、DIで注入可能に設計。
    /// PlayerLoopへの登録はアプリ側の責務。
    /// </summary>
    public sealed class SequencePlayer
    {
        private Sequence _sequence;
        private float _currentTime;
        private float _playbackSpeed = 1f;

        public bool IsPlaying { get; private set; }
        public bool IsLooping { get; set; }
        public float CurrentTime => _currentTime;

        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Math.Max(0.01f, value);
        }

        public Sequence Sequence
        {
            get => _sequence;
            set
            {
                _sequence = value;
                _currentTime = 0f;
                IsPlaying = false;
            }
        }

        // --- 再生状態イベント ---
        public event Action OnPlay;
        public event Action OnPause;
        public event Action OnStop;
        public event Action OnComplete;
        public event Action<float> OnTimeChanged;

        /// <summary>
        /// アクティブなクリップの再生情報を通知する。
        /// Update毎にアクティブな全クリップに対して発行される。
        /// アプリ側アダプターがこのイベントを受信し、クリップの種類に応じた
        /// 評価・適用を行う（AnimationFrame評価、FBXプレイヤー制御等）。
        /// </summary>
        public event Action<ClipPlaybackInfo> OnClipPlayback;

        public void Play()
        {
            if (_sequence == null) return;
            IsPlaying = true;
            OnPlay?.Invoke();
        }

        public void Pause()
        {
            IsPlaying = false;
            OnPause?.Invoke();
        }

        public void Stop()
        {
            IsPlaying = false;
            _currentTime = 0f;
            OnStop?.Invoke();
        }

        public void Seek(float time)
        {
            if (_sequence == null) return;
            _currentTime = Math.Clamp(time, 0f, _sequence.Duration.Duration);
            OnTimeChanged?.Invoke(_currentTime);
            EvaluateSequence(_currentTime);
        }

        /// <summary>
        /// フレーム更新。アプリ側のPlayerLoopまたはUpdate()から呼び出す。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsPlaying || _sequence == null) return;

            _currentTime += deltaTime * _playbackSpeed;

            var duration = _sequence.Duration.Duration;
            if (_currentTime > duration)
            {
                if (IsLooping)
                {
                    if (duration > 0f)
                        _currentTime %= duration;
                    else
                        _currentTime = 0f;
                }
                else
                {
                    _currentTime = duration;
                    IsPlaying = false;
                    OnComplete?.Invoke();
                }
            }

            OnTimeChanged?.Invoke(_currentTime);
            EvaluateSequence(_currentTime);
        }

        private void EvaluateSequence(float time)
        {
            if (_sequence == null) return;

            var currentTimeMs = (int)(time * 1000);
            foreach (var track in _sequence.Tracks)
            {
                var clip = track.GetActiveClipAt(currentTimeMs);
                if (clip?.ClipAsset == null) continue;

                var localTime = clip.GetLocalTime(time);
                OnClipPlayback?.Invoke(new ClipPlaybackInfo(
                    track.Id, track.TargetId, track.Type,
                    clip.Id, clip.ClipAsset, localTime));
            }
        }
    }
}
