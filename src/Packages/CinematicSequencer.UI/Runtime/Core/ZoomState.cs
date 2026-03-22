using System;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// ズーム状態の管理。ピクセル/秒の変換と、ズーム操作のハンドリング。
    /// </summary>
    public sealed class ZoomState
    {
        private float _pixelsPerSecond;
        private readonly float _minPixelsPerSecond;
        private readonly float _maxPixelsPerSecond;

        public float PixelsPerSecond => _pixelsPerSecond;

        public event Action<float> ZoomChanged;

        public ZoomState(float initial = 100f, float min = 20f, float max = 500f)
        {
            _minPixelsPerSecond = min;
            _maxPixelsPerSecond = max;
            _pixelsPerSecond = Clamp(initial);
        }

        /// <summary>
        /// ズーム倍率を変更。
        /// </summary>
        public void SetZoom(float pixelsPerSecond)
        {
            var clamped = Clamp(pixelsPerSecond);
            if (Math.Abs(clamped - _pixelsPerSecond) < float.Epsilon) return;
            _pixelsPerSecond = clamped;
            ZoomChanged?.Invoke(_pixelsPerSecond);
        }

        /// <summary>
        /// マウスホイールによるズーム。ポインタ位置を中心にズーム。
        /// </summary>
        /// <param name="delta">ホイールデルタ（正で拡大、負で縮小）</param>
        /// <param name="pivotTimeSeconds">ズーム中心の時刻（ポインタ位置から算出）</param>
        /// <returns>スクロール位置の補正量（ピクセル）</returns>
        public float ZoomAtPoint(float delta, float pivotTimeSeconds)
        {
            float oldPps = _pixelsPerSecond;
            float pivotPixelBefore = pivotTimeSeconds * oldPps;

            SetZoom(oldPps + delta);

            float pivotPixelAfter = pivotTimeSeconds * _pixelsPerSecond;
            return pivotPixelAfter - pivotPixelBefore;
        }

        public float TimeToPixels(float timeSeconds) => timeSeconds * _pixelsPerSecond;

        public float PixelsToTime(float pixels) => pixels / _pixelsPerSecond;

        private float Clamp(float value)
        {
            return value < _minPixelsPerSecond ? _minPixelsPerSecond
                 : value > _maxPixelsPerSecond ? _maxPixelsPerSecond
                 : value;
        }
    }
}
