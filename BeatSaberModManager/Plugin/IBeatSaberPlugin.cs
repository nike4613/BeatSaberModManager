using BeatSaberModManager.Utilities.Logging;
using System;

namespace BeatSaberModManager.Plugin
{
    public interface IBeatSaberPlugin
    {
        void Init(LoggerBase logger);

        Version Version { get; }

        void OnApplicationQuit();

        void OnApplicationStart();

        void OnFixedUpdate();

        void OnLevelWasInitialized(int level);

        void OnLevelWasLoaded(int level);

        void OnUpdate();
    }
}
