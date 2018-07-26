using BeatSaberModManager.Manager;
using BeatSaberModManager.Utilities.Logging;
using Harmony;
using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Logger = BeatSaberModManager.Utilities.Logging.Logger;

namespace BeatSaberModManager
{
    class ManagerPlugin : IPlugin
    {
        public static string GetName() => Assembly.GetCallingAssembly().GetName().Name;
        public string Name => GetName();

        public static Version GetVersion() => new Version(0,1,2,3); // hard code this so I don't need to change the assembly version
        public string Version => GetVersion().ToString();
        
        public static HarmonyInstance Harmony;

        static ManagerPlugin()
        {
#if DEBUG
            Logger.Filter = Logger.LogLevel.ReallyNotReccomendedAll;
#else
            Logger.Filter = Logger.LogLevel.All; // ??? 
#endif
            Application.logMessageReceived += delegate (string condition, string stackTrace, LogType type)
            {
                var level = UnityLogInterceptor.LogTypeToLevel(type);
                UnityLogInterceptor.Unitylogger.Log(level, $"{condition.Trim()}");
                UnityLogInterceptor.Unitylogger.Log(level, $"{stackTrace.Trim()}");
            };

            Harmony = HarmonyInstance.Create("com.cirr.beatsaber.modmanager");
            IPAPatches.IPAInject();
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
