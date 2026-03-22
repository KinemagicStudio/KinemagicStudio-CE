using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// クリップの移動＋リサイズ。PointerManipulator継承。
    /// v1 ClipElementDragAndDropManipulator を改良し、リサイズ対応を追加。
    /// スナッピングはController側で適用（Manipulatorは生の値を返す）。
    /// </summary>
    public sealed class ClipManipulator : PointerManipulator
    {
        public enum DragMode { None, Move, ResizeEnd }

        private const float ResizeHandleWidth = 8f;

        private readonly SnappingService _snapping;
        private readonly ZoomState _zoom;
        private readonly Func<VisualElement, TrackType, (Guid trackId, float trackY)?> _trackFinder;

        private DragMode _currentMode;
        private Vector3 _pointerStart;
        private Vector2 _targetStartTransform;
        private float _originalStartSeconds;
        private float _originalDurationSeconds;
        private bool _isDragging;

        /// <summary>
        /// クリップ移動完了。(clipId, oldTrackId, newTrackId, rawNewStartTime)
        /// </summary>
        public event Action<Guid, Guid, Guid, float> MoveCompleted;

        /// <summary>
        /// クリップリサイズ完了。(clipId, trackId, rawNewDuration)
        /// </summary>
        public event Action<Guid, Guid, float> ResizeCompleted;

        public bool Enabled { get; set; } = true;

        /// <param name="snapping">スナッピングサービス</param>
        /// <param name="zoom">ズーム状態</param>
        /// <param name="trackFinder">ドラッグ先のトラックを検索するデリゲート。
        /// VisualElement(ドラッグ中のクリップ)とTrackTypeを受け取り、
        /// 最も近いトラックのIdとY座標を返す。見つからなければnull。</param>
        public ClipManipulator(
            SnappingService snapping,
            ZoomState zoom,
            Func<VisualElement, TrackType, (Guid trackId, float trackY)?> trackFinder)
        {
            _snapping = snapping;
            _zoom = zoom;
            _trackFinder = trackFinder;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!Enabled || evt.button != 0) return;

            _currentMode = DetermineMode(evt.localPosition.x);
            if (_currentMode == DragMode.None) return;

            target.CapturePointer(evt.pointerId);
            _isDragging = true;
            _pointerStart = evt.position;
            _targetStartTransform = target.transform.position;

            float pps = _zoom.PixelsPerSecond;
            _originalStartSeconds = target.resolvedStyle.left / pps;
            _originalDurationSeconds = target.resolvedStyle.width / pps;

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

            var delta = evt.position - _pointerStart;

            switch (_currentMode)
            {
                case DragMode.Move:
                {
                    // transform.positionをオフセットとして使用（v1パターン踏襲）
                    // style.leftがベースポジションなので、transform.positionで
                    // ベースから負方向にはstyle.left分までしか動けない
                    float minX = -_originalStartSeconds * _zoom.PixelsPerSecond;
                    float newX = Mathf.Max(minX, _targetStartTransform.x + delta.x);
                    float newY = _targetStartTransform.y + delta.y;
                    target.transform.position = new Vector2(newX, newY);
                    break;
                }
                case DragMode.ResizeEnd:
                {
                    float pps = _zoom.PixelsPerSecond;
                    float minDurationPx = pps * 0.1f;
                    float newDurationPx = Mathf.Max(minDurationPx, _originalDurationSeconds * pps + delta.x);
                    target.style.width = newDurationPx;
                    break;
                }
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isDragging && target.HasPointerCapture(evt.pointerId))
            {
                target.ReleasePointer(evt.pointerId);
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!_isDragging) return;
            _isDragging = false;

            var clip = target as ClipElement;
            if (clip == null) return;

            switch (_currentMode)
            {
                case DragMode.Move:
                    CompleteMoveOperation(clip);
                    break;
                case DragMode.ResizeEnd:
                    CompleteResizeOperation(clip);
                    break;
            }

            _currentMode = DragMode.None;
        }

        private void CompleteMoveOperation(ClipElement clip)
        {
            var oldTrackId = clip.TrackId;
            var newTrackId = oldTrackId;
            float pps = _zoom.PixelsPerSecond;

            // ドラッグ先のトラック検索
            var found = _trackFinder?.Invoke(target, clip.TrackType);
            if (found.HasValue)
            {
                newTrackId = found.Value.trackId;
            }
            else
            {
                // 有効なトラックが見つからない場合は元の位置に戻す
                target.transform.position = Vector2.zero;
                return;
            }

            // 時間計算: 元の開始時刻 + transformオフセットから算出した時間差
            float rawNewStartTime = Mathf.Max(0f,
                _originalStartSeconds + target.transform.position.x / pps);

            // transform位置をリセット（ViewがUpdateFromModelで再配置する）
            target.transform.position = Vector2.zero;

            MoveCompleted?.Invoke(clip.ClipId, oldTrackId, newTrackId, rawNewStartTime);
        }

        private void CompleteResizeOperation(ClipElement clip)
        {
            float pps = _zoom.PixelsPerSecond;
            float rawNewDuration = Mathf.Max(0.1f, target.resolvedStyle.width / pps);

            // transform位置をリセット
            target.transform.position = Vector2.zero;

            ResizeCompleted?.Invoke(clip.ClipId, clip.TrackId, rawNewDuration);
        }

        private DragMode DetermineMode(float localX)
        {
            float elementWidth = target.resolvedStyle.width;
            if (elementWidth > 0 && localX > elementWidth - ResizeHandleWidth)
                return DragMode.ResizeEnd;
            return DragMode.Move;
        }
    }
}
