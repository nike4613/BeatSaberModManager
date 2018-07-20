using System;
using BeatSaberModManager.Meta;
using BeatSaberModManager.Plugin;
using BeatSaberModManager.Utilities.Logging;

namespace ModManagerTester
{
    [BeatSaberPlugin]
    class BrownCowPlugin : IBeatSaberPlugin
    {
        public static LoggerBase log;

        public Version Version => new Version(0,0,1,0);

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
        {
        }
    }
}
