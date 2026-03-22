using System;

namespace CinematicSequencer
{
    /// <summary>
    /// 外部モーションデータを参照するクリップアセット。
    /// FBXファイル等の外部ソースからモーションを再生する。
    /// キーフレーム編集は不可。シーケンス上での配置・リサイズのみ。
    /// </summary>
    public sealed class MotionClipAsset : IExternalClipAsset
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public TrackType Type => TrackType.Motion;

        /// <summary>
        /// モーションデータのソースパス（FBXファイルパス等）。
        /// アプリ側がこのパスを使ってFbxAnimationControllerを生成する。
        /// </summary>
        public string ExternalSourceId { get; set; }

        /// <summary>
        /// ソース内のクリップインデックス（FBXに複数アニメーションが含まれる場合）。
        /// </summary>
        public int ClipIndex { get; set; }

        /// <summary>
        /// キャッシュされたDuration。ソースファイルのロード時にアプリ側が設定する。
        /// </summary>
        public float CachedDuration { get; set; }

        public MotionClipAsset(string name, string externalSourceId, float cachedDuration = 0f)
        {
            Id = GuidExtensions.CreateVersion7();
            Name = name;
            ExternalSourceId = externalSourceId;
            CachedDuration = cachedDuration;
        }

        public MotionClipAsset(Guid id, string name, string externalSourceId, int clipIndex, float cachedDuration)
        {
            Id = id;
            Name = name;
            ExternalSourceId = externalSourceId;
            ClipIndex = clipIndex;
            CachedDuration = cachedDuration;
        }

        public float GetDuration() => CachedDuration;
    }
}
