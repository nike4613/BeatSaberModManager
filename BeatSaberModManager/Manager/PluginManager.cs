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
                typeof(PluginManager).GetMethod("LoadAssembly", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Assembly), typeof(Type[]) }, new ParameterModifier[] { });

            var toInject = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc_S, 4),
                new CodeInstruction(OpCodes.Call, internalLoad),
            };

            var codes = new List<CodeInstruction>(instructions);

            bool foundLoadFrom = false, foundGetTypes = false;
            int loadFromLoc = -1, getTypesLoc = -1;
            int injectLoc = -1, returnLoc = -1;

            for(int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (!foundLoadFrom)
                { // searching for first
                    if (code.opcode == OpCodes.Call && code.operand.Equals(loadFromInfo))
                    { // out first opcode
                        //Console.WriteLine($"Found call to Assembly.LoadFrom at {i}");
                        foundLoadFrom = true;
                        loadFromLoc = i;
                    }
                }
                else if (!foundGetTypes)
                { // searching for second
                    if (code.opcode == OpCodes.Callvirt && code.operand.Equals(getTypesInfo))
                    { // out first opcode
                        //Console.WriteLine($"Found call to assembly.GetTypes at {i}");
                        foundGetTypes = true;
                        getTypesLoc = i;
                    }
                }
                else if (injectLoc == -1)
                { // found our target
                  // code injection starts at getTypesLoc + 2
                    //Console.WriteLine($"Found code injection location at {getTypesLoc + 2}");
                    injectLoc = i = getTypesLoc + 2;
                    //Console.WriteLine($"{i} {getTypesLoc + 2} {loadFromLoc}");
                }
                else
                {
                    var start = i;
                    if (code.opcode == OpCodes.Ldloc_0 && codes[++i].opcode == OpCodes.Stloc_2)
                    { // found the cast
                        i += 2;
                        if (codes[i].opcode == OpCodes.Ldloc_2 && codes[++i].opcode == OpCodes.Ret)
                        {
                            //Console.WriteLine($"Found return at {start}");
                            returnLoc = start;
                        }
                    }
                }
            }

            toInject.Add(new CodeInstruction(OpCodes.Brtrue, returnLoc + toInject.Count));

            //Console.WriteLine($"{loadFromLoc} {getTypesLoc} {injectLoc} {returnLoc + toInject.Count}");

            codes.InsertRange(injectLoc, toInject);

            return codes.AsEnumerable();
        }
        private static void IPA_PluginComponent_OnApplicationQuit_Post()
        { // use this to update plugins
            Logger.log.Debug("Beat Saber shutting down...");
        }

        private static List<IBeatSaberPlugin> plugins = new List<IBeatSaberPlugin>();

        private static bool LoadAssembly(Assembly asm, Type[] types)
        {
            //Console.WriteLine(assm.FullName);
            object[] attrs = new object[] { };
            try
            {
                attrs = asm.GetCustomAttributes(typeof(BeatSaberModuleAttribute), false);
            }
            catch (TypeLoadException)
            {
                Logger.log.Warn($"Woah there buddy! Something is wrong with {asm.GetName().Name}! I can't read it's attributes!");

                return false;
            }

            if (attrs.Length == 0)
                return false;

            BeatSaberModuleAttribute moduleData = attrs.First() as BeatSaberModuleAttribute;

            Logger.log.Info($"Loading plugins from {moduleData.Name}");

            List<Type> modTypes = new List<Type>();

            foreach (var type in types)
            {
                Logger.log.SuperVerbose($"Checking {type.FullName}");
                if (type.IsDefined(typeof(BeatSaberPluginAttribute), false))
                { // marked a plugin, but is it actually?
                    Logger.log.SuperVerbose($"Found attribute on {type.FullName}");
                    if (type.GetInterfaces().Contains(typeof(IBeatSaberPlugin)))
                    { // also actually a plugin! great!
                        Logger.log.SuperVerbose($"Found plugin {type.FullName}");
                        modTypes.Add(type);
                    }
                }
            }

            string commonPrefix = modTypes.Select(t => t.FullName).FindCommonPrefix();

            int pluginsLoaded = 0;

            foreach (var type in modTypes)
            {
                string pluginFullName = $"{moduleData.Name}/{type.FullName.Replace(commonPrefix, "")}";

                try
                {
                    Logger.log.SuperVerbose($"Instantiating {type.FullName}");
                    IBeatSaberPlugin plugin = Activator.CreateInstance(type) as IBeatSaberPlugin;

                    LoggerBase logger = Logger.CreateLogger(modTypes.Count == 1 ? moduleData.Name : pluginFullName);
                    Logger.log.SuperVerbose($"Initializing {type.FullName}");
                    plugin.Init(logger);

                    plugins.Add(plugin);
                    pluginsLoaded++;
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Cannot initialize plugin {pluginFullName}");
                    Logger.log.Debug(e);
                }
            }

            Logger.log.SuperVerbose($"Plugins loaded from {moduleData.Name}: {pluginsLoaded}");

            if (pluginsLoaded == 0) {
                Logger.log.Error($"No plugins could be loaded from {asm.GetName().Name} ({moduleData.Name}).");
                return false;
            }
            return true;
        }
    }
}
