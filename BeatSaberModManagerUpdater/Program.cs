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
            if (args.Length < 2)
            {
                throw new Exception("Invalid call");
            }
            
            var oldFile = args[1];
            var pluginDir = Path.GetDirectoryName(oldFile);
            var newFile = Path.Combine(pluginDir, outputName);
            var parentPid = int.Parse(args[0]);

            var parent = Process.GetProcessById(parentPid);

            Console.WriteLine($"Waiting for parent ({parentPid}) process to die...");

            parent.WaitForExit();

            Console.WriteLine($"Replacing {oldFile}");

            if (File.Exists(oldFile)) File.Delete(oldFile);
            if (File.Exists(newFile)) File.Delete(newFile);
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BeatSaberModManagerUpdater.Resources.BeatSaberModManager.dll"))
                using (FileStream fstream = File.Create(newFile))
                    stream.CopyTo(fstream);

            Console.WriteLine("Done (press enter to exit)");

            if (!args.Contains("noconfirm"))
                Console.ReadLine();
        }
    }
}
