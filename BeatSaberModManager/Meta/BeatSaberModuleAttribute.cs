using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BeatSaberModManager.Meta
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public class BeatSaberModuleAttribute : Attribute
    {
        private readonly static string NoUpdateUriUri = "http://www.cirr.com/this-isnt-a-real-update-site";

        public string Name { get; private set; }
        public Uri UpdateUri { get; private set; }

        public BeatSaberModuleAttribute(string name, string updateUri = null)
        {
            UpdateUri = new Uri(updateUri ?? NoUpdateUriUri);
            Name = name;
        }
    }
}
