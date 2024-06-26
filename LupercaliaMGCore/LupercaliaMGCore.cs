﻿using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace LupercaliaMGCore {
    public class LupercaliaMGCore: BasePlugin
    {
        public static readonly string PLUGIN_PREFIX  = $" {ChatColors.DarkRed}[{ChatColors.Blue}LPŘ MG{ChatColors.DarkRed}]{ChatColors.Default}";

        public static string MessageWithPrefix(string message) {
            return $"{PLUGIN_PREFIX} {message}";
        }

        private static LupercaliaMGCore? instance;

        public static LupercaliaMGCore getInstance() {
            return instance!;
        }

        public override string ModuleName => "Lupercalia MG Core";

        public override string ModuleVersion => "0.1.0";

        public override string ModuleAuthor => "faketuna";

        public override string ModuleDescription => "Provides core MG feature in CS2 with CounterStrikeSharp";


        public override void Load(bool hotReload)
        {
            instance = this;
            new PluginSettings(this);
            Logger.LogInformation("Plugin settings initialized");

            new TeamBasedBodyColor(this);
            Logger.LogInformation("TBBC initialized");

            new DuckFix(this, hotReload);
            Logger.LogInformation("DFix initialized");

            new TeamScramble(this);
            Logger.LogInformation("TeamScramble initialized");

            new VoteMapRestart(this);
            Logger.LogInformation("VoteMapRestart initialized");

            new VoteRoundRestart(this);
            Logger.LogInformation("VoteRoundRestart initialized");
            
            new RoundEndDamageImmunity(this);
            Logger.LogInformation("RoundEndDamageImmunity initialized");

            new RoundEndWeaponStrip(this);
            Logger.LogInformation("RoundEndWeaponStrip initialized");

            new ScheduledShutdown(this);
            Logger.LogInformation("ScheduledShutdown initialized");

            new Respawn(this);
            Logger.LogInformation("Respawn initialized");

            new MapConfig(this);
            Logger.LogInformation("MapConfig initialized");

            new AntiCamp(this, hotReload);
            Logger.LogInformation("Anti Camp initialized");

            new Omikuji(this);
            Logger.LogInformation("Omikuji initialized");

            new Debugging(this);
            Logger.LogInformation("Debugging feature is initialized");

            new MiscCommands(this);
            Logger.LogInformation("misc commands initialized");
        }
    }
}