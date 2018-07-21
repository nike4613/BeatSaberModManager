using System;
using BeatSaberModManager.Meta;
using BeatSaberModManager.Plugin;
using BeatSaberModManager.Utilities.Logging;

namespace ModManagerTester
{
    [BeatSaberPlugin("BrownCowPlugin", "file://Z:/Users/aaron/Documents/Visual%20Studio%202017/Projects/BeatSaberModManager/ModManagerTester/update_script.json")]
    class BrownCowPlugin : IBeatSaberPlugin
    {
        public static LoggerBase log;

        public Version Version => new Version(0,0,1,2);

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
