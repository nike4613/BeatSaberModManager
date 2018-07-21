using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatSaberModManager.Meta
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BeatSaberPluginAttribute : Attribute
    {
        public string Name { get; private set; }
        public Uri UpdateUri { get; private set; }

        public BeatSaberPluginAttribute(string name)
        {
            UpdateUri = null; // updateUri == null ? new Uri(updateUri) : null;
            Name = name;
        }
        public BeatSaberPluginAttribute(string name, string updateUri)
        {
            UpdateUri = updateUri != null ? new Uri(updateUri) : null;
            Name = name;
        }
    }
}
