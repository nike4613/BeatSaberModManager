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

        public string Version => Assembly.GetCallingAssembly().GetName().Version.ToString();
        
        public static HarmonyInstance Harmony;

        static ManagerPlugin()
        {
            Logger.Filter = Logger.LogLevel.ReallyNotReccomendedAll;

            Harmony = HarmonyInstance.Create("com.cirr.beatsaber.modmanager");
            PluginManager.IPAInject();
        }

        public void OnApplicationQuit()
        {

        }

        public void OnApplicationStart()
        {
            Console.WriteLine(Name);
            Console.WriteLine(Version);
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
