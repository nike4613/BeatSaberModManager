using BeatSaberModManager.Meta;
using BeatSaberModManager.Plugin;
using BeatSaberModManager.Utilities;
using BeatSaberModManager.Utilities.Logging;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace BeatSaberModManager.Manager
{
    public static class PluginManager
    {
        public static void IPAInject()
        { // yes, we are injecting ourselves into IPA at runtime
            var harmony = ManagerPlugin.Harmony;

            var loadPluginsFromFile = typeof(IllusionInjector.PluginManager).GetMethod("LoadPluginsFromFile", BindingFlags.NonPublic | BindingFlags.Static);
            var onApplicationQuit = typeof(IllusionInjector.PluginComponent).GetMethod("OnApplicationQuit", BindingFlags.NonPublic | BindingFlags.Instance);
            var loadPluginsFromFileTranspiler = typeof(PluginManager).GetMethod("IPA_LoadPlugins_Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            var onApplicationQuitPost = typeof(PluginManager).GetMethod("IPA_PluginComponent_OnApplicationQuit_Post", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(loadPluginsFromFile, null, null, new HarmonyMethod(loadPluginsFromFileTranspiler));
            harmony.Patch(onApplicationQuit, null, new HarmonyMethod(onApplicationQuitPost));
        }

        private static IEnumerable<CodeInstruction> IPA_LoadPlugins_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo loadFromInfo = typeof(Assembly).GetMethod("LoadFrom", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, new ParameterModifier[] { });
            MethodInfo getTypesInfo = typeof(Assembly).GetMethod("GetTypes", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { }, new ParameterModifier[] { });
            MethodInfo internalLoad = 
                typeof(PluginManager).GetMethod("LoadAssembly", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Type[]) }, new ParameterModifier[] { });

            var toInject = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, internalLoad),
            };

            var codes = new List<CodeInstruction>(instructions);
            
            int injectLoc = -1, returnLoc = -1;

            for(int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.opcode == OpCodes.Call && code.operand.Equals(loadFromInfo))
                { // first call
                    code = codes[++i];
                    if (code.opcode == OpCodes.Callvirt && code.operand.Equals(getTypesInfo))
                    { // second call
                        code = codes[++i];
                        if (code.opcode == OpCodes.Stloc_1)
                        { // store return value
                            // heres where we want to inject
                            injectLoc = ++i;
                        }
                    }
                }
                else
                {
                    var start = i;
                    if (code.opcode == OpCodes.Ldloc_0 && codes[++i].opcode == OpCodes.Stloc_2)
                    { // found the cast
                        i += 2;
                        if (codes[i].opcode == OpCodes.Ldloc_2 && codes[++i].opcode == OpCodes.Ret)
                        {
                            returnLoc = start;
                        }
                    }
                }
            }

            toInject.Add(new CodeInstruction(OpCodes.Brtrue, returnLoc + toInject.Count));

            //Console.WriteLine($"{injectLoc} {returnLoc + toInject.Count}");

            codes.InsertRange(injectLoc, toInject);

            return codes.AsEnumerable();
        }
        private static void IPA_PluginComponent_OnApplicationQuit_Post()
        { // use this to update plugins
            Logger.log.Debug("Beat Saber shutting down...");
        }

        private static List<Tuple<string, IBeatSaberPlugin, BeatSaberPluginAttribute>> plugins = new List<Tuple<string, IBeatSaberPlugin, BeatSaberPluginAttribute>>();

        public static void OnApplicationStart()
        {
            int longlen = 0;
            var foundstr = $"Found {plugins.Count} mods";
            longlen = foundstr.Length;

            IEnumerable<string> namestrings = plugins.Select(plugin => $"{plugin.Item1} {plugin.Item2.Version}");
            int longname = namestrings.Select(s => s.Length).Max();
            longlen = longlen < longname ? longname : longlen;
            string dashstr = new string('-', longlen);

            string center(string s)
            {
                int spaces = longname - s.Length;
                int padLeft = spaces / 2 + s.Length;
                return s.PadLeft(padLeft);
            }

            Logger.log.Debug(dashstr);
            Logger.log.Debug(center(foundstr));
            Logger.log.Debug(dashstr);
            foreach (var name in namestrings)
                Logger.log.Debug(center(name));
            Logger.log.Debug(dashstr);

            foreach (var plugin in plugins)
                plugin.Item2.OnApplicationStart();
        }
        public static void OnApplicationQuit()
        {
            foreach (var plugin in plugins)
                plugin.Item2.OnApplicationQuit();
        }
        public static void OnFixedUpdate()
        {
            foreach (var plugin in plugins)
                plugin.Item2.OnFixedUpdate();
        }
        public static void OnUpdate()
        {
            foreach (var plugin in plugins)
                plugin.Item2.OnUpdate();
        }
        public static void OnLevelWasInitialized(int index)
        {
            foreach (var plugin in plugins)
                plugin.Item2.OnLevelWasInitialized(index);
        }
        public static void OnLevelWasLoaded(int index)
        {
            foreach (var plugin in plugins)
                plugin.Item2.OnLevelWasLoaded(index);
        }

        private static bool LoadAssembly(Type[] types)
        {
            var asm = types.First().Assembly;

            Logger.log.SuperVerbose($"Checking for plugins in {asm.GetName().Name}");

            List<Tuple<Type, BeatSaberPluginAttribute>> modTypes = new List<Tuple<Type, BeatSaberPluginAttribute>>();

            foreach (var type in types)
            {
                Logger.log.SuperVerbose($"Checking {type.FullName}");
                if (type.IsDefined(typeof(BeatSaberPluginAttribute), false))
                { // marked a plugin, but is it actually?
                    Logger.log.SuperVerbose($"Found attribute on {type.FullName}");
                    if (type.GetInterfaces().Contains(typeof(IBeatSaberPlugin)))
                    { // also actually a plugin! great!
                        Logger.log.SuperVerbose($"Found plugin {type.FullName}");
                        modTypes.Add(new Tuple<Type, BeatSaberPluginAttribute>(type, type.GetCustomAttribute<BeatSaberPluginAttribute>()));
                    }
                }
            }

            if (modTypes.Count == 0)
            {
                //Logger.log.Warn($"Assembly {asm.GetName().Name} has the BeatSaberModule attribute, but defines no mods!");
                return false;
            }

            string commonPrefix = modTypes.Select(t => t.Item1.FullName).FindCommonPrefix();

            int pluginsLoaded = 0;

            foreach (var type in modTypes)
            {
                string pluginFullName = $"{asm.GetName().Name}/{type.Item1.FullName.Replace(commonPrefix, "")}";

                string finalName = modTypes.Count == 1 ? asm.GetName().Name : pluginFullName;
                finalName = type.Item2.Name ?? finalName;

                try
                {
                    Logger.log.SuperVerbose($"Instantiating {finalName} ({type.Item1.FullName})");
                    IBeatSaberPlugin plugin = Activator.CreateInstance(type.Item1) as IBeatSaberPlugin;

                    LoggerBase logger = Logger.CreateLogger(finalName);
                    Logger.log.SuperVerbose($"Initializing {finalName}");
                    plugin.Init(logger);

                    plugins.Add(new Tuple<string, IBeatSaberPlugin, BeatSaberPluginAttribute>(finalName, plugin, type.Item2));
                    pluginsLoaded++;
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Cannot initialize plugin {finalName}");
                    Logger.log.Debug(e);
                }
            }

            Logger.log.SuperVerbose($"Plugins loaded from {asm.GetName().Name}: {pluginsLoaded}");

            if (pluginsLoaded == 0) {
                Logger.log.Error($"No plugins could be loaded from {asm.GetName().Name}.");
                return false;
            }
            return true;
        }
    }
}
