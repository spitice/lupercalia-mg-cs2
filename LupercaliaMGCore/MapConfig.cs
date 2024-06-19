using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace LupercaliaMGCore {
    public class MapConfig {
        private LupercaliaMGCore m_CSSPlugin;

        // Config name and path
        private List<MapConfigFile> configs = new List<MapConfigFile>();

        private string configFolder;

        public MapConfig(LupercaliaMGCore plugin) {
            m_CSSPlugin = plugin;
            
            configFolder = Path.GetFullPath(Path.Combine(Server.GameDirectory, PluginSettings.CONFIG_FOLDER, "map/"));

            if(!checkDirectoryExists())
                throw new InvalidOperationException("Map config directory is not exists and failed to create.");

            updateConfigsDictionary();

            m_CSSPlugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart, HookMode.Post);
            m_CSSPlugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
        }

        private HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info) {            
            if(!MathUtil.DecomposePowersOfTwo(PluginSettings.getInstance.m_CVMapConfigExecutionTiming.Value).Contains(2))
                return HookResult.Continue;

            SimpleLogging.LogDebug("[Map Config] Executing configs at round PreStart.");
            executeConfigs();
            return HookResult.Continue;
        }

        private void OnMapStart(string mapName) {
            if(!MathUtil.DecomposePowersOfTwo(PluginSettings.getInstance.m_CVMapConfigExecutionTiming.Value).Contains(1))
                return;
            
            SimpleLogging.LogDebug("[Map Config] Executing configs at map start.");
            executeConfigs();
        }

        private void executeConfigs() {
            SimpleLogging.LogTrace("[Map Config] Updating the config dictionary");
            updateConfigsDictionary();

            int mapCfgType = PluginSettings.getInstance.m_CVMapConfigType.Value;

            SimpleLogging.LogTrace("[Map Config] Checking the Map Config type");
            if(mapCfgType == 0) {
                SimpleLogging.LogTrace("[Map Config] mapCfgType is 0. cancelling the execution.");
                return;
            }

            string? mapName = Server.MapName;

            if(mapName == null) {
                SimpleLogging.LogTrace("[Map Config] mapName is null. cancelling the execution.");
                return;
            }


            SimpleLogging.LogTrace("[Map Config] Iterating the config file");
            foreach(MapConfigFile conf in configs) {
                SimpleLogging.LogTrace("[Map Config] Checking the map config type");

                if(MathUtil.DecomposePowersOfTwo(mapCfgType).Contains(2) && !mapName.StartsWith(conf.name))
                    continue;
                
                
                SimpleLogging.LogTrace("[Map Config] Checking the map config type 2");
                
                SimpleLogging.LogTrace($"{conf.name} {conf.path}");

                if(MathUtil.DecomposePowersOfTwo(mapCfgType).Contains(1) && !mapName.Equals(conf.name))
                    continue;

                SimpleLogging.LogTrace($"[Map Config] Executing config {conf.name} located at {conf.path}");
                Server.ExecuteCommand($"exec {conf.path}");
            }
        }

        private void updateConfigsDictionary() {
            SimpleLogging.LogTrace("[Map Config] Get files from directory");
            string[] files = Directory.GetFiles(configFolder, "", SearchOption.TopDirectoryOnly);

            SimpleLogging.LogTrace("[Map Config] Clearing configs");
            configs.Clear();
            SimpleLogging.LogTrace("[Map Config] Iterating files");
            foreach(string file in files) {
                string fileName = Path.GetFileName(file);
                if(!fileName.EndsWith(".cfg", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                string relativePath = Path.GetRelativePath(Path.GetFullPath(Path.Combine(Server.GameDirectory, "csgo/cfg/")), file);

                configs.Add(new MapConfigFile(fileName[..fileName.LastIndexOf(".")], relativePath));
                SimpleLogging.LogTrace($"[Map Config] Adding config {configs.Last().name}, {configs.Last().path}");
            }
        }

        private bool checkDirectoryExists() {
            if(!Directory.Exists(configFolder)) {
                try {
                    m_CSSPlugin.Logger.LogWarning($"Map config folder {configFolder} is not exists. Trying to create...");

                    Directory.CreateDirectory(configFolder);
                } catch(Exception e) {
                    m_CSSPlugin.Logger.LogError($"Failed to create map config folder!");
                    m_CSSPlugin.Logger.LogError(e.StackTrace);
                    return false;
                }
            }

            return true;
        }
    }
}