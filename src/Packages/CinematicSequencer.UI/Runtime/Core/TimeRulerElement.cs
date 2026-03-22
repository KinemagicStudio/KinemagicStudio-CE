using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// タイムルーラー。目盛りの生成、ズーム対応、クリックによる時間設定を行う。
    /// SequenceEditorとKeyframeEditorの両方から共有される。
    /// </summary>
    public sealed class TimeRulerElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TimeRulerElement> { }

        private static readonly float[] TickSteps = { 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 15f, 30f, 60f };
        private const float TargetMajorTickPixelSpacing = 100f;
        private const float RulerHeight = 24f;
        private const float MajorTickHeight = 14f;
        private const float MinorTickHeight = 8f;

        private float _totalDuration;
        private float _pixelsPerSecond;
        private bool _isDragging;

        /// <summary>クリック/ドラッグで時間を選択</summary>
        public event Action<float> TimeClicked;

        public TimeRulerElement()
        {
            style.height = RulerHeight;
            style.overflow = Overflow.Hidden;

            generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        public void SetDuration(float duration)
        {
            _totalDuration = Mathf.Max(0f, duration);
            style.width = _totalDuration * _pixelsPerSecond;
            MarkDirtyRepaint();
        }

        public void SetZoom(float pixelsPerSecond)
        {
            _pixelsPerSecond = Mathf.Max(1f, pixelsPerSecond);
            style.width = _totalDuration * _pixelsPerSecond;
            MarkDirtyRepaint();
        }

        private (float major, float minor) CalculateTickIntervals()
        {
            float rawInterval = TargetMajorTickPixelSpacing / _pixelsPerSecond;
            float major = TickSteps[TickSteps.Length - 1];
            foreach (var step in TickSteps)
            {
                if (step >= rawInterval)
                {
                    major = step;
                    break;
                }
            }
            float minor = major / 4f;
            return (major, minor);
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_pixelsPerSecond <= 0f || _totalDuration <= 0f) return;

            var painter = mgc.painter2D;
            var (majorInterval, minorInterval) = CalculateTickIntervals();
            float totalWidth = _totalDuration * _pixelsPerSecond;
            float height = resolvedStyle.height;

            // Minor ticks
            painter.strokeColor = new Color(1f, 1f, 1f, 0.2f);
            painter.lineWidth = 1f;
            for (float t = 0f; t <= _totalDuration + minorInterval * 0.5f; t += minorInterval)
            {
                float x = t * _pixelsPerSecond;
                if (x > totalWidth) break;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, height - MinorTickHeight));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }

            // Major ticks
            painter.strokeColor = new Color(1f, 1f, 1f, 0.5f);
            painter.lineWidth = 1f;
            for (float t = 0f; t <= _totalDuration + majorInterval * 0.5f; t += majorInterval)
            {
                float x = t * _pixelsPerSecond;
                if (x > totalWidth) break;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, height - MajorTickHeight));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            this.CapturePointer(evt.pointerId);
            _isDragging = true;
            NotifyTimeAtPosition(evt.localPosition.x);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !this.HasPointerCapture(evt.pointerId)) return;
            NotifyTimeAtPosition(evt.localPosition.x);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging && this.HasPointerCapture(evt.pointerId))
            {
                this.ReleasePointer(evt.pointerId);
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            _isDragging = false;
        }

        private void NotifyTimeAtPosition(float localX)
        {
            if (_pixelsPerSecond <= 0f) return;
            float time = Mathf.Max(0f, localX / _pixelsPerSecond);
            TimeClicked?.Invoke(time);
        }
    }
}
