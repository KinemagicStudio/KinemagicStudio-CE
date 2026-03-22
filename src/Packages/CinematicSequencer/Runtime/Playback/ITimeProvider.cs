namespace CinematicSequencer.Playback
{
    /// <summary>
    /// 時間の取得を抽象化するインターフェース。
    /// テスト時にモック可能。
    /// </summary>
    public interface ITimeProvider
    {
        float DeltaTime { get; }
    }
}
