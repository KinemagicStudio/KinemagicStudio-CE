using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// 再生ヘッド（タイムカーソル）。
    /// ルーラー上のヘッドと、トラック領域を貫通する縦線の2パーツ。
    /// </summary>
    public sealed class PlayheadElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PlayheadElement> { }

        private const float HeadWidth = 12f;
        private const float HeadHeight = 10f;
        private const float LineWidth = 2f;

        private float _currentTime;
        private float _pixelsPerSecond;
        private bool _isDragging;

        private readonly VisualElement _headPart;
        private readonly VisualElement _linePart;

        /// <summary>ドラッグで時間を変更</summary>
        public event Action<float> TimeDragged;

        public PlayheadElement()
        {
            style.position = Position.Absolute;
            style.width = 0;
            pickingMode = PickingMode.Ignore;

            _headPart = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    width = HeadWidth,
                    height = HeadHeight,
                    left = -HeadWidth / 2f,
                    top = 0,
                    backgroundColor = new Color(0.2f, 0.68f, 1f, 1f),
                },
                pickingMode = PickingMode.Position,
            };
            _headPart.generateVisualContent += DrawHead;
            Add(_headPart);

            _linePart = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    width = LineWidth,
                    left = -LineWidth / 2f,
                    top = HeadHeight,
                    bottom = 0,
                    backgroundColor = new Color(0.2f, 0.68f, 1f, 0.8f),
                },
                pickingMode = PickingMode.Ignore,
            };
            Add(_linePart);

            SetupDragManipulator();
        }

        public void SetTime(float time)
        {
            _currentTime = Mathf.Max(0f, time);
            UpdatePosition();
        }

        public void SetZoom(float pixelsPerSecond)
        {
            _pixelsPerSecond = Mathf.Max(1f, pixelsPerSecond);
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            float x = _currentTime * _pixelsPerSecond;
            style.left = x;
        }

        private void SetupDragManipulator()
        {
            _headPart.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _headPart.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _headPart.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _headPart.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            _headPart.CapturePointer(evt.pointerId);
            _isDragging = true;
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !_headPart.HasPointerCapture(evt.pointerId)) return;

            // Convert to parent-local coordinates for time calculation
            var parentLocal = parent.WorldToLocal(evt.position);
            float time = Mathf.Max(0f, parentLocal.x / _pixelsPerSecond);
            _currentTime = time;
            UpdatePosition();
            TimeDragged?.Invoke(time);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging && _headPart.HasPointerCapture(evt.pointerId))
            {
                _headPart.ReleasePointer(evt.pointerId);
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            _isDragging = false;
        }

        private static void DrawHead(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            painter.fillColor = new Color(0.2f, 0.68f, 1f, 1f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(HeadWidth, 0));
            painter.LineTo(new Vector2(HeadWidth / 2f, HeadHeight));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
