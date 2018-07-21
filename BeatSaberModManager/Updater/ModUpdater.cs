using BeatSaberModManager.Manager;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Logger = BeatSaberModManager.Utilities.Logging.Logger;

namespace BeatSaberModManager.Updater
{
    class ModUpdater : MonoBehaviour
    {
        public ModUpdater instance;

        public void Awake()
        {
            instance = this;
            CheckForUpdates();
        }

        public void CheckForUpdates()
        {
            StartCoroutine(CheckForUpdatesCoroutine());
        }

        struct UpdateCheckQueueItem
        {
            public PluginManager.PluginObject Plugin;
            public Uri UpdateUri;
            public string Name;
        }

        struct UpdateQueueItem
        {
            public PluginManager.PluginObject Plugin;
            public Uri DownloadUri;
            public string Name;
            public Version NewVersion;
        }

        private Regex commentRegex = new Regex(@"(?: \/\/.+)?$", RegexOptions.Compiled | RegexOptions.Multiline);
        private Dictionary<Uri, UpdateScript> cachedRequests = new Dictionary<Uri, UpdateScript>();
        IEnumerator CheckForUpdatesCoroutine()
        {
            Logger.log.Info("Checking for mod updates...");

            var toUpdate = new List<UpdateQueueItem>();
            var plugins = new Queue<UpdateCheckQueueItem>(PluginManager.Plugins.Select(p => new UpdateCheckQueueItem { Plugin = p, UpdateUri = p.Meta.UpdateUri, Name = p.Meta.Name }));

            for (; plugins.Count > 0 ;)
            {
                var plugin = plugins.Dequeue();

                Logger.log.SuperVerbose($"Checking for {plugin.Name}");

                if (plugin.UpdateUri != null)
                {
                    Logger.log.SuperVerbose($"Has update uri '{plugin.UpdateUri}'");
                    if (!cachedRequests.ContainsKey(plugin.UpdateUri))
                        using (var request = UnityWebRequest.Get(plugin.UpdateUri))
                        {
                            Logger.log.SuperVerbose("Getting resource");

                            yield return request.SendWebRequest();

                            if (request.isNetworkError)
                            {
                                Logger.log.Error("Network error while trying to update plugins");
                                Logger.log.Error(request.error);
                                break;
                            }
                            if (request.isHttpError)
                            {
                                Logger.log.Error($"Server returned an error code while trying to update plugin {plugin.Name}");
                                Logger.log.Error(request.error);
                            }

                            Logger.log.SuperVerbose("Resource gotten");

                            var json = request.downloadHandler.text;

                            json = commentRegex.Replace(json, "");

                            JSONObject obj = null;
                            try
                            {
                                obj = JSON.Parse(json).AsObject;
                            }
                            catch (InvalidCastException)
                            {
                                Logger.log.Error($"Parse error while trying to update plugin {plugin.Name}");
                                Logger.log.Error($"Response doesn't seem to be a JSON object");
                                continue;
                            }
                            catch (Exception e)
                            {
                                Logger.log.Error($"Parse error while trying to update pluging {plugin.Name}");
                                Logger.log.Error(e);
                                continue;
                            }

                            UpdateScript ss;
                            try
                            {
                                ss = UpdateScript.Parse(obj);
                            }
                            catch (Exception e)
                            {
                                Logger.log.Error($"Parse error while trying to update plugin {plugin.Name}");
                                Logger.log.Error($"Script at {plugin.UpdateUri} doesn't seem to be a valid update script");
                                Logger.log.Debug(e);
                                continue;
                            }

                            cachedRequests.Add(plugin.UpdateUri, ss);
                        }

                    var script = cachedRequests[plugin.UpdateUri];
                    if (script.Info.TryGetValue(plugin.Name, out UpdateScript.PluginVersionInfo info))
                    {
                        Logger.log.SuperVerbose($"Checking version info for {plugin.Name} ({plugin.Plugin.Meta.Name})");
                        if (info.NewName != null || info.NewScript != null)
                            plugins.Enqueue(new UpdateCheckQueueItem
                            {
                                Plugin = plugin.Plugin,
                                Name = info.NewName ?? plugin.Name,
                                UpdateUri = info.NewScript ?? plugin.UpdateUri
                            });
                        else
                        {
                            Logger.log.SuperVerbose($"New version: {info.Version}, Current version: {plugin.Plugin.Plugin.Version}");
                            if (info.Version > plugin.Plugin.Plugin.Version)
                            { // we should update plugin
                                Logger.log.Debug($"Queueing update for {plugin.Name} ({plugin.Plugin.Meta.Name})");

                                toUpdate.Add(new UpdateQueueItem
                                {
                                    Plugin = plugin.Plugin,
                                    DownloadUri = info.Download,
                                    Name = plugin.Name,
                                    NewVersion = info.Version
                                });
                            }
                        }
                    }
                    else
                    {
                        Logger.log.Error($"Script defined for plugin {plugin.Name} doesn't define information for {plugin.Name}");
                        continue;
                    }
                }
            }

            Logger.log.Info($"{toUpdate.Count} plugins need updating");

            if (toUpdate.Count == 0) yield break;

            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            Logger.log.SuperVerbose($"Created temp download dirtectory {tempDirectory}");
            foreach (var item in toUpdate)
            {
                Logger.log.SuperVerbose($"Starting coroutine to download {item.Name}");
                StartCoroutine(DownloadPluginCoroutine(tempDirectory, item));
            }
        }

        IEnumerator DownloadPluginCoroutine(string tempdir, UpdateQueueItem item)
        {
            var file = Path.Combine(tempdir, item.Name + ".dll");

            using (var req = UnityWebRequest.Get(item.DownloadUri))
            {
                req.downloadHandler = new DownloadHandlerFile(file);
                yield return req.SendWebRequest();

                if (req.isNetworkError)
                {
                    Logger.log.Error($"Network error while trying to download update for {item.Plugin.Name}");
                    Logger.log.Error(req.error);
                    yield break;
                }
                if (req.isHttpError)
                {
                    Logger.log.Error($"Server returned an error code while trying to download update for {item.Plugin.Name}");
                    Logger.log.Error(req.error);
                    yield break;
                }

                Logger.log.SuperVerbose("Finished download of new file");
            }

            var pluginDir = Path.GetDirectoryName(item.Plugin.FileName);
            var newFile = Path.Combine(pluginDir, item.Name + ".dll");

            Logger.log.SuperVerbose($"Moving downloaded file to {newFile}");

            File.Delete(item.Plugin.FileName);
            if (File.Exists(newFile))
                File.Delete(newFile);
            File.Move(file, newFile);

            Logger.log.Info($"{item.Plugin.Name} updated to {item.NewVersion}");
        }

        /** // JSON format
         * {
         *   "_updateScript": "0.1",            // version
         *   "<pluginName>": {                  // an entry for your plugin, using its annotated name
         *     "version": "<version>",          // required, should be in .NET Version class format
         *                                      // note: only required if neither newName nor newScript is specified
         *     "newName": "<newName>",          // optional, defines a new name for the plugin (gets saved under this name) 
         *                                      // (updater will also check this file for this name to get latest)
         *     "newScript": "<newScript>",      // optional, defines a new location for the update script
         *                                      // updater will look here for latest version too
         *                                      // note: if both newName and newScript are defined, the updater will only look in newScript
         *                                      //       for newName, and not any other combination
         *     "download": "<url>",             // required, defines URL to use for downloading new version
         *                                      // note: only required if neither newName nor newScript is specified
         *   },
         *   ...
         * }
         */

        class UpdateScript
        {
            static readonly Version ScriptVersion = new Version(0, 1);

            public Version Version { get; private set; }

            private Dictionary<string, PluginVersionInfo> info = new Dictionary<string, PluginVersionInfo>();
            public IReadOnlyDictionary<string, PluginVersionInfo> Info { get => info; }

            public class PluginVersionInfo
            {
                public Version Version { get; protected internal set; }
                public string NewName { get; protected internal set; }
                public Uri NewScript { get; protected internal set; }
                public Uri Download { get; protected internal set; }
            }

            public static UpdateScript Parse(JSONObject jscript)
            {
                var script = new UpdateScript
                {
                    Version = Version.Parse(jscript["_updateScript"].Value)
                };
                if (script.Version != ScriptVersion)
                    throw new UpdateScriptParseException("Script version mismatch");

                jscript.Remove("_updateScript");

                foreach (var kvp in jscript)
                {
                    var obj = kvp.Value.AsObject;
                    var pvi = new PluginVersionInfo
                    {
                        Version = obj.Linq.Any(p => p.Key == "version") ? Version.Parse(obj["version"].Value) : null,
                        Download = obj.Linq.Any(p => p.Key == "download") ? new Uri(obj["download"].Value) : null,

                        NewName = obj.Linq.Any(p => p.Key == "newName") ? obj["newName"] : null,
                        NewScript = obj.Linq.Any(p => p.Key == "newScript") ? new Uri(obj["newScript"]) : null
                    };
                    if (pvi.NewName == null && pvi.NewScript == null && (pvi.Version == null || pvi.Download == null))
                        throw new UpdateScriptParseException($"Required fields missing from object {kvp.Key}");

                    script.info.Add(kvp.Key, pvi);
                }

                return script;
            }

            [Serializable]
            private class UpdateScriptParseException : Exception
            {
                public UpdateScriptParseException()
                {
                }

                public UpdateScriptParseException(string message) : base(message)
                {
                }

                public UpdateScriptParseException(string message, Exception innerException) : base(message, innerException)
                {
                }

                protected UpdateScriptParseException(SerializationInfo info, StreamingContext context) : base(info, context)
                {
                }
            }
        }
    }
}
