using BeatSaberModManager.Manager;
using BeatSaberModManager.Utilities.Logging;
using Harmony;
using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BeatSaberModManager
{
    class ManagerPlugin : IPlugin
    {
        public static string GetName() => Assembly.GetCallingAssembly().GetName().Name;
        public string Name => GetName();

        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public static HarmonyInstance Harmony;

        static ManagerPlugin()
        {
            Logger.Filter = Logger.LogLevel.ReallyNotReccomendedAll;

            Harmony = HarmonyInstance.Create("com.cirr.beatsaber.modmanager");
            PluginManager.IPAInject();
        }

        public void OnApplicationQuit()
        {
            PluginManager.OnApplicationQuit();
        }

        public void OnApplicationStart()
        {
            Logger.log.Info($"{Name} version {Version}");
            PluginManager.OnApplicationStart();
        }

        public void OnFixedUpdate()
        {
            PluginManager.OnFixedUpdate();
        }

        public void OnLevelWasInitialized(int level)
        {
            PluginManager.OnLevelWasInitialized(level);
        }

        public void OnLevelWasLoaded(int level)
        {
            PluginManager.OnLevelWasLoaded(level);
        }

        public void OnUpdate()
        {
            PluginManager.OnUpdate();
        }
    }
}
