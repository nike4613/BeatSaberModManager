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
using System.Threading.Tasks;

namespace BeatSaberModManager.Manager
{
    static class IPAPatches
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
            var loadPluginsFromFileTranspiler = typeof(IPAPatches).GetMethod("IPA_LoadPluginsFile_Transpiler", BindingFlags.NonPublic | BindingFlags.Static);
            var onApplicationQuit = typeof(IllusionInjector.PluginComponent).GetMethod("OnApplicationQuit", BindingFlags.NonPublic | BindingFlags.Instance);
            var onApplicationQuitPost = typeof(IPAPatches).GetMethod("IPA_PluginComponent_OnApplicationQuit_Post", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(loadPluginsFromFile, null, null, new HarmonyMethod(loadPluginsFromFileTranspiler));
            harmony.Patch(onApplicationQuit, null, new HarmonyMethod(onApplicationQuitPost));
        }

        private static IEnumerable<CodeInstruction> IPA_LoadPluginsFile_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            MethodInfo loadFromInfo = typeof(Assembly).GetMethod("LoadFrom", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, new ParameterModifier[] { });
            MethodInfo getTypesInfo = typeof(Assembly).GetMethod("GetTypes", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { }, new ParameterModifier[] { });
            MethodInfo internalLoad =
                typeof(PluginManager).GetMethod("LoadAssembly", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Type[]), typeof(string) }, new ParameterModifier[] { });
            MethodInfo movePlugin = typeof(PluginManager).GetMethod("MovePlugin", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string) }, new ParameterModifier[] { });

            var branchLabel = generator.DefineLabel();

            var toInject = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, internalLoad),
                new CodeInstruction(OpCodes.Brtrue, branchLabel)
            };

            var codes = new List<CodeInstruction>(instructions);

            bool setLabel = false;

            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.opcode == OpCodes.Call && code.operand.Equals(loadFromInfo))
                { // first call
                    var inject = i;
                    code = codes[++i];
                    if (code.opcode == OpCodes.Callvirt && code.operand.Equals(getTypesInfo))
                    { // second call
                        code = codes[++i];
                        if (code.opcode == OpCodes.Stloc_1)
                        { // store return value
                            // heres where we want to inject
                            codes.InsertRange(++i, toInject);
                            codes.Insert(inject, new CodeInstruction(OpCodes.Call, movePlugin));
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

            return codes.AsEnumerable();
        }

        internal static event Action OnAfterApplicationQuit;
        private static void IPA_PluginComponent_OnApplicationQuit_Post()
        {
            Logger.log.Debug("Beat Saber shutting down...");
            if (OnAfterApplicationQuit != null)
            {
                OnAfterApplicationQuit();
            }
        }
    }
}
