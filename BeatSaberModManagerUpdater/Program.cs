using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberModManagerUpdater
{
    class Program
    {
        const string outputName = "!BeatSaberModManager.dll";
        static void Main(string[] args)
        {
            do
            { // allow me to break
                if (args.Length < 2)
                { // called by user to install
                    Console.WriteLine("Finding Beat Saber install directory");

                    var path = GetSteamLocation();
                    path = path ?? GetOculusHomeLocation();
                    if (path == null)
                    {
                        Console.WriteLine("Beat Saber not found");
                        break;
                    }

                    var pluginDir = Path.Combine(path, "Plugins");
                    if (!Directory.Exists(pluginDir))
                    {
                        Console.WriteLine("It seems like you don't have IPA installed. This won't work without it.");
                        Directory.CreateDirectory(pluginDir);
                    }

                    Console.WriteLine("Installing mod manager");

                    InstallFile(pluginDir);
                }
                else
                { // called by BeatSaber to self-update
                    var oldFile = args[1];
                    var pluginDir = Path.GetDirectoryName(oldFile);
                    var parentPid = int.Parse(args[0]);

                    try
                    { // wait for beat saber to exit (ensures we can modify the file)
                        var parent = Process.GetProcessById(parentPid);

                        Console.WriteLine($"Waiting for parent ({parentPid}) process to die...");

                        parent.WaitForExit();
                    }
                    catch (Exception) { }

                    Console.WriteLine($"Replacing {oldFile}");

                    // install file to beat saber
                    InstallFile(pluginDir, oldFile);
                }
            }
            while (false);

            Console.WriteLine("Done (press enter to exit)");

            if (!args.Contains("noconfirm"))
                Console.ReadLine();
        }

        static void InstallFile(string pluginDir, string replace = null)
        {
            var newFile = Path.Combine(pluginDir, outputName);
            if (replace != null && File.Exists(replace)) File.Delete(replace);
            if (File.Exists(newFile)) File.Delete(newFile);

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatSaberModManagerUpdater.Resources.BeatSaberModManager.dll"))
            using (FileStream fstream = File.Create(newFile))
                stream.CopyTo(fstream);
        }

        // copied from Umbranoxio's BeatSaberModInstaller
        private static string GetSteamLocation()
        {
            string path = RegistryWOW6432.GetRegKey64(RegHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980", @"InstallLocation");
            if (path != null)
            {
                path = path + @"\";
            }
            return path;
        }
        private static string GetOculusHomeLocation()
        {
            string path = RegistryWOW6432.GetRegKey32(RegHive.HKEY_LOCAL_MACHINE, @"SOFTWARE\Oculus VR, LLC\Oculus\Config", @"InitialAppLibrary");
            if (path != null)
            {
                path = path + @"\Software\hyperbolic-magnetism-beat-saber";
            }
            return path;
        }
    }
}
