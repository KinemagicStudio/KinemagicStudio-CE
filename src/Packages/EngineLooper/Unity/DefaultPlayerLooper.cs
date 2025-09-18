using System;
using System.Collections.Generic;
using System.Linq;
using EngineLooper.Unity.PlayerLoopUtility;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace EngineLooper.Unity
{
    public static class DefaultPlayerLooper
    {
        static readonly PlayerLoopRunner[] _loopRunners = new PlayerLoopRunner[(int)DefaultPlayerLoop.LoopTiming.Count];
        static readonly Dictionary<object, PlayerLoopItem> _disposables = new();

        static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (_initialized) return;

            UnityEngine.Debug.Log($"$<color=cyan>[{nameof(DefaultPlayerLooper)}] Initialize</color>");

            Application.quitting += Quit;
            InitializePlayerLoopSystem();

            _initialized = true;
        }

        static void Quit()
        {
            UnityEngine.Debug.Log($"$<color=cyan>[{nameof(DefaultPlayerLooper)}] Quit</color>");

            foreach (var loopRunner in _loopRunners)
            {
                loopRunner.Clear();
            }

            foreach (var disposable in _disposables.Values)
            {
                disposable.Action.Invoke();
            }

            _disposables.Clear();
        }

        public static void Register(object target)
        {
            if (target is IInitializable initializable)
            {
                Register(target, initializable.Initialize, DefaultPlayerLoop.LoopTiming.PreInitialization);
            }

            // if (target is IPostInitializable postInitializable)
            // {
            //     Register(target, postInitializable.PostInitialize, DefaultPlayerLoop.LoopTiming.PostInitialization);
            // }

            if (target is IStartable startable)
            {
                Register(target, startable.Start, DefaultPlayerLoop.LoopTiming.PreStartup);
            }

            // if (target is IPostStartable postStartable)
            // {
            //     Register(target, postStartable.PostStart, DefaultPlayerLoop.LoopTiming.PostStartup);
            // }

            // if (target is IFixedTickable fixedTickable)
            // {
            //     Register(target, fixedTickable.FixedTick, DefaultPlayerLoop.LoopTiming.PreBehaviourFixedUpdate);
            // }

            // if (target is IPostFixedTickable postFixedTickable)
            // {
            //     Register(target, postFixedTickable.PostFixedTick, DefaultPlayerLoop.LoopTiming.PostBehaviourFixedUpdate);
            // }

            if (target is ITickable Tickable)
            {
                Register(target, Tickable.Tick, DefaultPlayerLoop.LoopTiming.PreBehaviourUpdate);
            }

            // if (target is IPostTickable postTickable)
            // {
            //     Register(target, postTickable.PostTick, DefaultPlayerLoop.LoopTiming.PostBehaviourUpdate);
            // }

            if (target is ILateTickable lateTickable)
            {
                Register(target, lateTickable.LateTick, DefaultPlayerLoop.LoopTiming.PreBehaviourLateUpdate);
            }

            // if (target is IPostLateTickable postLateTickable)
            // {
            //     Register(target, postLateTickable.PostLateTick, DefaultPlayerLoop.LoopTiming.PostBehaviourLateUpdate);
            // }
        }

        public static void Register(Action action, DefaultPlayerLoop.LoopTiming timing)
        {
            Register(action.Target, action, timing);
        }

        public static void Register(object target, Action action, DefaultPlayerLoop.LoopTiming timing)
        {
            _loopRunners[(int)timing].Register(target, action);

            if (target is IDisposable disposable)
            {
                if (!_disposables.ContainsKey(target))
                {
                    var playerLoopItem = new PlayerLoopItem(target, disposable.Dispose);
                    _disposables.Add(playerLoopItem.Target, playerLoopItem);
                }
            }
        }

        public static void Unregister(object target, DefaultPlayerLoop.LoopTiming timing)
        {
            _loopRunners[(int)timing].Unregister(target);
        }

        public static void Unregister(object target)
        {
            foreach (var loopRunner in _loopRunners)
            {
                loopRunner.Unregister(target);
            }

            if (_disposables.TryGetValue(target, out var disposable))
            {
                disposable.Action.Invoke();
                _disposables.Remove(target);
            }
        }

        static void InitializePlayerLoopSystem()
        {
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreTimeUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostTimeUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreInitialization] = new PlayerLoopRunner(true);
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostInitialization] = new PlayerLoopRunner(true);
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreStartup] = new PlayerLoopRunner(true);
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostStartup] = new PlayerLoopRunner(true);
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreBehaviourFixedUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostBehaviourFixedUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreBehaviourUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostBehaviourUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreBehaviourLateUpdate] = new PlayerLoopRunner();
            _loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostBehaviourLateUpdate] = new PlayerLoopRunner();

            var playerLoopSystem = PlayerLoop.GetCurrentPlayerLoop();
            var subSystemList = playerLoopSystem.subSystemList.ToArray();

            int timeUpdateSystemIndex = PlayerLoopHelper.FindLoopSystemIndex(typeof(TimeUpdate), subSystemList);
            int initializationSystemIndex = PlayerLoopHelper.FindLoopSystemIndex(typeof(Initialization), subSystemList);
            int earlyUpdateSystemIndex = PlayerLoopHelper.FindLoopSystemIndex(typeof(EarlyUpdate), subSystemList);
            int fixedUpdateSystemIndex = PlayerLoopHelper.FindLoopSystemIndex(typeof(FixedUpdate), subSystemList);
            int updateSystemIndex = PlayerLoopHelper.FindLoopSystemIndex(typeof(Update), subSystemList);
            int preLateUpdateSystemIndex = PlayerLoopHelper.FindLoopSystemIndex(typeof(PreLateUpdate), subSystemList);

            ref var timeUpdateSystem = ref subSystemList[timeUpdateSystemIndex];
            ref var initializationSystem = ref subSystemList[initializationSystemIndex];
            ref var earlyUpdateSystem = ref subSystemList[earlyUpdateSystemIndex];
            ref var fixedUpdateSystem = ref subSystemList[fixedUpdateSystemIndex];
            ref var updateSystem = ref subSystemList[updateSystemIndex];
            ref var preLateUpdateSystem = ref subSystemList[preLateUpdateSystemIndex];

            var preTimeUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPreTimeUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreTimeUpdate].Run);

            var postTimeUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPostTimeUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostTimeUpdate].Run);

            var preInitializationSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPreInitialization>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreInitialization].Run);

            var postInitializationSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPostInitialization>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostInitialization].Run);

            var preStartupSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPreStartup>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreStartup].Run);

            var postStartupSystem =
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPostStartup>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostStartup].Run);

            var preBehaviourFixedUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPreBehaviourFixedUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreBehaviourFixedUpdate].Run);

            var postBehaviourFixedUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPostBehaviourFixedUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostBehaviourFixedUpdate].Run);

            var preBehaviourUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPreBehaviourUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreBehaviourUpdate].Run);

            var postBehaviourUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPostBehaviourUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostBehaviourUpdate].Run);

            var preBehaviourLateUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPreBehaviourLateUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PreBehaviourLateUpdate].Run);

            var postBehaviourLateUpdateSystem = 
                PlayerLoopHelper.CreateLoopSystem<DefaultPlayerLoopPostBehaviourLateUpdate>(_loopRunners[(int)DefaultPlayerLoop.LoopTiming.PostBehaviourLateUpdate].Run);

            PlayerLoopHelper.InsertSubSystem(ref timeUpdateSystem, 0, ref preTimeUpdateSystem);
            PlayerLoopHelper.AppendSubSystem(ref timeUpdateSystem, ref postTimeUpdateSystem);

            PlayerLoopHelper.InsertSubSystem(ref initializationSystem, 0, ref preInitializationSystem);
            PlayerLoopHelper.AppendSubSystem(ref initializationSystem, ref postInitializationSystem);

            PlayerLoopHelper.InsertSubSystem(ref earlyUpdateSystem, typeof(EarlyUpdate.ScriptRunDelayedStartupFrame), PlayerLoopHelper.InsertPosition.Before, ref preStartupSystem);
            PlayerLoopHelper.InsertSubSystem(ref earlyUpdateSystem, typeof(EarlyUpdate.ScriptRunDelayedStartupFrame), PlayerLoopHelper.InsertPosition.After, ref postStartupSystem);

            PlayerLoopHelper.InsertSubSystem(ref fixedUpdateSystem, typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate), PlayerLoopHelper.InsertPosition.Before, ref preBehaviourFixedUpdateSystem);
            PlayerLoopHelper.InsertSubSystem(ref fixedUpdateSystem, typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate), PlayerLoopHelper.InsertPosition.After, ref postBehaviourFixedUpdateSystem);

            PlayerLoopHelper.InsertSubSystem(ref updateSystem, typeof(Update.ScriptRunBehaviourUpdate), PlayerLoopHelper.InsertPosition.Before, ref preBehaviourUpdateSystem);
            PlayerLoopHelper.InsertSubSystem(ref updateSystem, typeof(Update.ScriptRunBehaviourUpdate), PlayerLoopHelper.InsertPosition.After, ref postBehaviourUpdateSystem);

            PlayerLoopHelper.InsertSubSystem(ref preLateUpdateSystem, typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), PlayerLoopHelper.InsertPosition.Before, ref preBehaviourLateUpdateSystem);
            PlayerLoopHelper.InsertSubSystem(ref preLateUpdateSystem, typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), PlayerLoopHelper.InsertPosition.After, ref postBehaviourLateUpdateSystem);

            playerLoopSystem.subSystemList = subSystemList;
            PlayerLoop.SetPlayerLoop(playerLoopSystem);
        }
    }
}
