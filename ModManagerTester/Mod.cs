using BeatSaberModManager.Meta;
using BeatSaberModManager.Plugin;
using BeatSaberModManager.Utilities.Logging;

namespace ModManagerTester
{
    [BeatSaberPlugin]
    class BrownCowPlugin : IBeatSaberPlugin
    {
        public static LoggerBase log;

        public void Init(LoggerBase logger)
        {
            log = logger;
            log.Critical("HOW NOW BROWN COW");
        }

        public void OnApplicationQuit()
        {
        }

        public void OnApplicationStart()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {;
        }
    }
    [BeatSaberPlugin]
    class FuckYouPlugin : IBeatSaberPlugin
    {
        public static LoggerBase log;

        public void Init(LoggerBase logger)
        {
            log = logger;
            log.Critical("HOW NOW FUCK YOU");
        }

        public void OnApplicationQuit()
        {
        }

        public void OnApplicationStart()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
            ;
        }
    }
}
