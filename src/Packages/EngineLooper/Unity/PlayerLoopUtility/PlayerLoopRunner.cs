using System;
using System.Collections.Generic;

namespace EngineLooper.Unity.PlayerLoopUtility
{
    sealed class PlayerLoopItem
    {
        public object Target { get; }
        public Action Action { get; }

        public PlayerLoopItem(object target, Action action)
        {
            Target = target;
            Action = action;
        }
    }

    public sealed class PlayerLoopRunner
    {
        readonly List<PlayerLoopItem> _playerLoopItems = new();
        readonly bool _autoClear;

        public PlayerLoopRunner(bool autoClear = false)
        {
            _autoClear = autoClear;
        }

        public void Run()
        {
            foreach (var item in _playerLoopItems)
            {
                item.Action.Invoke();
            }

            if (_autoClear)
            {
                _playerLoopItems.Clear();
            }
        }

        public void Register(object target, Action action)
        {
            if (_playerLoopItems.Find(item => item.Target == target && item.Action == action) != null)
            {
                throw new InvalidOperationException("Already registered");
            }
            _playerLoopItems.Add(new PlayerLoopItem(target, action));
        }

        public void Unregister(object target)
        {
            int targetIndex = _playerLoopItems.FindIndex(item => item.Target == target);
            if (targetIndex != -1)
            {
                _playerLoopItems.RemoveAt(targetIndex);
            }
        }

        public void Clear()
        {
            _playerLoopItems.Clear();
        }
    }
}
