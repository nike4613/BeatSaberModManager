using BeatSaberModManager.Utilities.Logging;

namespace BeatSaberModManager.Plugin
{
    public interface IBeatSaberPlugin
    {
        void Init(LoggerBase logger);

        void OnApplicationQuit();

        void OnApplicationStart();

        void OnFixedUpdate();

        void OnLevelWasInitialized(int level);

        void OnLevelWasLoaded(int level);

        void OnUpdate();
    }
}
