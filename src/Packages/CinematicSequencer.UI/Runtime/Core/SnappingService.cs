using System;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// スナッピング機能。クリップの移動・リサイズ時に
    /// グリッドや他のクリップ端にスナップさせる。
    /// </summary>
    public sealed class SnappingService
    {
        [Flags]
        public enum SnapMode
        {
            None = 0,
            Grid = 1,
            ClipEdges = 2,
        }

        public SnapMode Mode { get; set; } = SnapMode.Grid | SnapMode.ClipEdges;
        public float GridInterval { get; set; } = 1f;
        public float SnapThresholdPixels { get; set; } = 10f;

        /// <summary>
        /// 時刻をスナッピング。
        /// </summary>
        /// <param name="rawTime">ドラッグ中の生の時刻</param>
        /// <param name="sequence">スナップ対象（他クリップの端）を取得するためのシーケンス</param>
        /// <param name="excludeClipId">自分自身を除外</param>
        /// <param name="pixelsPerSecond">スナップ閾値の計算用</param>
        /// <returns>スナップ後の時刻</returns>
        public float Snap(float rawTime, Sequence sequence, Guid? excludeClipId, float pixelsPerSecond)
        {
            if (Mode == SnapMode.None || pixelsPerSecond <= 0f) return rawTime;

            float thresholdSeconds = SnapThresholdPixels / pixelsPerSecond;
            float bestTime = rawTime;
            float bestDistance = thresholdSeconds;

            if ((Mode & SnapMode.Grid) != 0 && GridInterval > 0f)
            {
                float snapped = MathF.Round(rawTime / GridInterval) * GridInterval;
                float dist = MathF.Abs(snapped - rawTime);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestTime = snapped;
                }
            }

            if ((Mode & SnapMode.ClipEdges) != 0 && sequence != null)
            {
                foreach (var track in sequence.Tracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        if (excludeClipId.HasValue && clip.Id == excludeClipId.Value) continue;

                        float startDist = MathF.Abs(clip.Placement.Start - rawTime);
                        if (startDist < bestDistance)
                        {
                            bestDistance = startDist;
                            bestTime = clip.Placement.Start;
                        }

                        float endDist = MathF.Abs(clip.Placement.End - rawTime);
                        if (endDist < bestDistance)
                        {
                            bestDistance = endDist;
                            bestTime = clip.Placement.End;
                        }
                    }
                }
            }

            return bestTime;
        }
    }
}
