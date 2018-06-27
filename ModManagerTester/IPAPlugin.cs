using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModManagerTester
{
    public class IPAPlugin : IPlugin
    {
        public string Name => "Mod Manager Tester";

        public string Version => "v -1";

        static IPAPlugin()
        {
            Console.WriteLine("This should never be visible");
        }

        public IPAPlugin()
        {
            Console.WriteLine("This should never be visible");
        }

        public void OnApplicationQuit()
        {
        }

        public void OnApplicationStart()
        {
            Console.WriteLine("This should never be visible");
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
