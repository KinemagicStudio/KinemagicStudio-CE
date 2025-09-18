using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;

namespace CinematicSequencer
{
    public static class CinematicSequenceSystem
    {
        static TimelinePlayer _sequencePlayer;

        public static bool IsInitialized => _sequencePlayer != null;
        public static TimelinePlayer SequencePlayer
        {
            get
            {
                if (!IsInitialized)
                {
                    _sequencePlayer = new TimelinePlayer();
                }
                return _sequencePlayer;
            }
        }

        public static void SetSequencePlayer(TimelinePlayer player)
        {
            _sequencePlayer = player;
        }

        static void Update()
        {
            _sequencePlayer?.Update(Time.deltaTime);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InsertPlayerLoopSystem()
        {
            // Append a custom system to the Early Update phase.

            var customSystem = new PlayerLoopSystem()
            {
                type = typeof(CinematicSequenceSystem),
                updateDelegate = () => CinematicSequenceSystem.Update()
            };

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                ref var phase = ref playerLoop.subSystemList[i];
                if (phase.type == typeof(UnityEngine.PlayerLoop.EarlyUpdate))
                {
                    phase.subSystemList = phase.subSystemList.Concat(new[]{ customSystem }).ToArray();
                    break;
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }
    }
}