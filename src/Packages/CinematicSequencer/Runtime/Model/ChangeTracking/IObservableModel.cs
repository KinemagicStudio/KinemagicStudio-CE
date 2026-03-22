using System;

namespace CinematicSequencer
{
    /// <summary>
    /// モデルの変更通知を発行するインターフェース。
    /// </summary>
    public interface IObservableModel
    {
        event Action<ModelChangeEvent> Changed;
    }
}
