using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// 再生コントロールツールバー。Play/Pause/Stopボタンと時間表示。
    /// </summary>
    public sealed class PlaybackToolbar : VisualElement
    {
        private readonly Button _playButton;
        private readonly Button _pauseButton;
        private readonly Button _stopButton;
        private readonly Label _timeLabel;

        public event Action OnPlayClicked;
        public event Action OnPauseClicked;
        public event Action OnStopClicked;

        public PlaybackToolbar()
        {
            AddToClassList("playback-toolbar");

            _playButton = new Button(() => OnPlayClicked?.Invoke()) { text = "Play" };
            _playButton.AddToClassList("playback-toolbar__button");
            _playButton.AddToClassList("playback-toolbar__play");
            Add(_playButton);

            _pauseButton = new Button(() => OnPauseClicked?.Invoke()) { text = "Pause" };
            _pauseButton.AddToClassList("playback-toolbar__button");
            _pauseButton.AddToClassList("playback-toolbar__pause");
            _pauseButton.SetEnabled(false);
            Add(_pauseButton);

            _stopButton = new Button(() => OnStopClicked?.Invoke()) { text = "Stop" };
            _stopButton.AddToClassList("playback-toolbar__button");
            _stopButton.AddToClassList("playback-toolbar__stop");
            _stopButton.SetEnabled(false);
            Add(_stopButton);

            _timeLabel = new Label(FormatTime(0f));
            _timeLabel.AddToClassList("playback-toolbar__time");
            Add(_timeLabel);
        }

        public void UpdateTime(float timeSeconds)
        {
            _timeLabel.text = FormatTime(timeSeconds);
        }

        public void UpdatePlaybackState(bool isPlaying)
        {
            _playButton.SetEnabled(!isPlaying);
            _pauseButton.SetEnabled(isPlaying);
            _stopButton.SetEnabled(isPlaying);
        }

        /// <summary>
        /// 時刻フォーマット: MM:SS.mmm (v1 FormatTime を流用)
        /// </summary>
        private static string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            int milliseconds = Mathf.FloorToInt((timeInSeconds * 1000) % 1000);
            return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        }
    }
}
