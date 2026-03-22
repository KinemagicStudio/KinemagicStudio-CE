namespace CinematicSequencer
{
    /// <summary>
    /// 外部再生ソースを参照するクリップアセット。
    /// シーケンサーは再生タイミングのみを制御し、実際の評価は
    /// アプリ側アダプターが外部プレイヤーに委譲する。
    /// FBXモーション、Audio等が該当。
    /// </summary>
    public interface IExternalClipAsset : IClipAsset
    {
        /// <summary>
        /// 外部データソースの識別子（ファイルパス、アセットID等）。
        /// アプリ側アダプターがこの値を使って外部プレイヤーを特定・生成する。
        /// </summary>
        string ExternalSourceId { get; }
    }
}
