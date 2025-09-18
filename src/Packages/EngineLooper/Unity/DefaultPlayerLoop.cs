namespace EngineLooper.Unity
{
    public static class DefaultPlayerLoop
    {
        public enum LoopTiming
        {
            PreTimeUpdate = 0,
            PostTimeUpdate = 1,

            PreInitialization = 2,
            PostInitialization = 3,

            PreStartup = 4,
            PostStartup = 5,

            PreBehaviourFixedUpdate = 6,
            PostBehaviourFixedUpdate = 7,

            PreBehaviourUpdate = 8,
            PostBehaviourUpdate = 9,

            PreBehaviourLateUpdate  = 10,
            PostBehaviourLateUpdate = 11,

            Count = 12,
        }
    }

    struct DefaultPlayerLoopPreTimeUpdate {}
    struct DefaultPlayerLoopPostTimeUpdate {}
    struct DefaultPlayerLoopPreInitialization {}
    struct DefaultPlayerLoopPostInitialization {}
    struct DefaultPlayerLoopPreStartup {}
    struct DefaultPlayerLoopPostStartup {}
    struct DefaultPlayerLoopPreBehaviourFixedUpdate {}
    struct DefaultPlayerLoopPostBehaviourFixedUpdate {}
    struct DefaultPlayerLoopPreBehaviourUpdate {}
    struct DefaultPlayerLoopPostBehaviourUpdate {}
    struct DefaultPlayerLoopPreBehaviourLateUpdate {}
    struct DefaultPlayerLoopPostBehaviourLateUpdate {}
}
