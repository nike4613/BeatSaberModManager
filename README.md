# Beat Saber Mod Manager
## Updating
In order to enable your plugin for auto-updating, you need 2 things: an "update script", and the BeatSaberPluginAttribute.updateUri argument.
An update script is a JSON file containing a single object of the following format:
```javascript
{
  "_updateScript": "0.1",            // version
  "<pluginName>": {                  // an entry for your plugin, using its annotated name
    "version": "<version>",          // required, should be in .NET Version class format
                                     // note: only required if neither newName nor newScript is specified
    "newName": "<newName>",          // optional, defines a new name for the plugin (gets saved under this name) 
                                     // (updater will also check this file for this name to get latest)
    "newScript": "<newScript>",      // optional, defines a new location for the update script
                                     // updater will look here for latest version too
                                     // note: if both newName and newScript are defined, the updater will only look in newScript
                                     //       for newName, and not any other combination
    "download": "<url>",             // required, defines URL to use for downloading new version
                                     // note: only required if neither newName nor newScript is specified
  },
  ...								 //  more entries
}
```
You specify a URL pointing to one of the files in the annotation like:
```csharp
[BeatSaberPlugin("ExamplePlugin", "http://example.com/path/to/script.json")]
class ExamplePlugin : IBeatSaberPlugin
{
```