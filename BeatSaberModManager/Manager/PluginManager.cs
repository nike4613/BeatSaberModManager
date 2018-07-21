using BeatSaberModManager.Meta;
using BeatSaberModManager.Plugin;
using BeatSaberModManager.Updater;
using BeatSaberModManager.Utilities;
using BeatSaberModManager.Utilities.Logging;
using Harmony;
using Harmony.ILCopying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Logger = BeatSaberModManager.Utilities.Logging.Logger;

namespace BeatSaberModManager.Manager
{
    public static class PluginManager
    {
        public static string AsString(this CodeInstruction t)
        {
            List<string> list = new List<string>();
            foreach (Label label in t.labels)
            {
                list.Add("Label" + label.GetHashCode());
            }
            foreach (ExceptionBlock block in t.blocks)
            {
                list.Add("EX_" + block.blockType.ToString().Replace("Block", ""));
            }
            string arg = (list.Count > 0) ? (" [" + string.Join(", ", list.ToArray()) + "]") : "";
            string text = Emitter.FormatArgument(t.operand);
            if (text != "")
            {
                text = " " + text;
            }
            return t.opcode + text + arg;
        }

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

        private static IEnumerable<CodeInstruction> IPA_LoadPlugins_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo readAllBytes = typeof(File).GetMethod("ReadAllBytes", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, new ParameterModifier[] { });
            MethodInfo loadBytes = typeof(Assembly).GetMethod("Load", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(byte[]) }, new ParameterModifier[] { });
            MethodInfo loadFromInfo = typeof(Assembly).GetMethod("LoadFrom", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, new ParameterModifier[] { });
            MethodInfo getTypesInfo = typeof(Assembly).GetMethod("GetTypes", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { }, new ParameterModifier[] { });
            MethodInfo internalLoad = 
                typeof(PluginManager).GetMethod("LoadAssembly", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Type[]), typeof(string) }, new ParameterModifier[] { });

            var branchLabel = generator.DefineLabel();

            var toInject = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, internalLoad),
                new CodeInstruction(OpCodes.Brtrue, branchLabel)
            };

            var codes = new List<CodeInstruction>(instructions);

            int mainInjectLoc = -1;
            bool setLabel = false;

            for(int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.opcode == OpCodes.Call && code.operand.Equals(loadFromInfo))
                { // first call
                    var firstCall = code;
                    var inject = i;
                    code = codes[++i];
                    if (code.opcode == OpCodes.Callvirt && code.operand.Equals(getTypesInfo))
                    { // second call
                        code = codes[++i];
                        if (code.opcode == OpCodes.Stloc_1)
                        { // store return value
                            // inject code to not hold handle to file
                            codes.Insert(inject, new CodeInstruction(OpCodes.Call, readAllBytes));
                            firstCall.operand = loadBytes;
                            i++;

                            // heres where we want to inject
                            mainInjectLoc = ++i;
                        }
                    }
                }
                else
                {
                    if (code.opcode == OpCodes.Ldloc_0 && codes[++i].opcode == OpCodes.Ret && !setLabel)
                    {
                        code.labels.Add(branchLabel);
                        setLabel = true;
                    }
                }
            }

            codes.InsertRange(mainInjectLoc, toInject);

            // Logger.log.Debug(string.Join("\n", codes.Select(c => c.AsString())));

            return codes.AsEnumerable();
        }
        private static void IPA_PluginComponent_OnApplicationQuit_Post()
        { // use this to update plugins
            Logger.log.Debug("Beat Saber shutting down...");
        }

        public class PluginObject
        {
            public string FileName { get; internal set; }
            public string Name { get; internal set; }
            public IBeatSaberPlugin Plugin { get; internal set; }
            public BeatSaberPluginAttribute Meta { get; internal set; }
        }

        private static List<PluginObject> plugins = new List<PluginObject>();
        public static List<PluginObject> Plugins => plugins;

        public static void OnApplicationStart()
        {
            int longlen = 0;
            var foundstr = $"Found {plugins.Count} mods";
            longlen = foundstr.Length;

            IEnumerable<string> namestrings = plugins.Select(plugin => $"{plugin.Name} {plugin.Plugin.Version}");
            int longname = 0;
            if (namestrings.Count() > 0)
                longname = namestrings.Select(s => s.Length).Max();
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
            {
                try
                {
                    plugin.Plugin.OnApplicationStart();
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error during OnApplicationStart of plugin {plugin.Name}");
                    Logger.log.Error(e);
                }
            }

            new GameObject("Mod Updater").AddComponent<ModUpdater>();
        }

        internal static event Action OnPluginsUnloaded;
        public static void OnApplicationQuit()
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Plugin.OnApplicationQuit();
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error during OnApplicationQuit of plugin {plugin.Name}");
                    Logger.log.Error(e);
                }
            }

            OnPluginsUnloaded();
        }
        public static void OnFixedUpdate()
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Plugin.OnFixedUpdate();
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error during OnFixedUpdate of plugin {plugin.Name}");
                    Logger.log.Error(e);
                }
            }
        }
        public static void OnUpdate()
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Plugin.OnUpdate();
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error during OnUpdate of plugin {plugin.Name}");
                    Logger.log.Error(e);
                }
            }
        }
        public static void OnLevelWasInitialized(int index)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Plugin.OnLevelWasInitialized(index);
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error during OnLevelWasInitialized of plugin {plugin.Name}");
                    Logger.log.Error(e);
                }
            }
        }
        public static void OnLevelWasLoaded(int index)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Plugin.OnLevelWasLoaded(index);
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error during OnLevelWasLoaded of plugin {plugin.Name}");
                    Logger.log.Error(e);
                }
            }
        }

        private static bool LoadAssembly(Type[] types, string filename)
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

                    plugins.Add(new PluginObject
                    {
                        FileName = filename,
                        Name = finalName,
                        Plugin = plugin,
                        Meta = type.Item2
                    });
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
