using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using TNCSSPluginFoundation.Models.Plugin;

namespace LupercaliaMGCore.modules;

public sealed class RoundEndDamageImmunity(IServiceProvider serviceProvider) : PluginModuleBase(serviceProvider)
{
    public override string PluginModuleName => "RoundEndDamageImmunity";
    
    public override string ModuleChatPrefix => "[RoundEndDamageImmunity]";
    protected override bool UseTranslationKeyInModuleChatPrefix => false;

    private bool damageImmunity = false;

    
    public readonly FakeConVar<bool> IsModuleEnabled = new("lp_mg_redi_enabled",
        "Should player grant damage immunity after round end until next round starts.", false);
    
    protected override void OnInitialize()
    {
        TrackConVar(IsModuleEnabled);
        
        Plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
        Plugin.RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        Plugin.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
    }

    protected override void OnUnloadModule()
    {
        Plugin.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Pre);
        Plugin.DeregisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        Plugin.DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (damageImmunity && IsModuleEnabled.Value)
        {
            var player = @event.Userid?.PlayerPawn.Value;

            if (player == null)
                return HookResult.Continue;

            // LastHealth has been removed (Verified in CSSharp v1.0.342)
            //player.Health = player.LastHealth;
            // just add back the damage to the player's health
            player.Health += @event.DmgHealth;
            DebugLogger.LogTrace($"[Round End Damage Immunity] [Player {player.Controller.Value?.PlayerName}] Nullified damage");
            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        damageImmunity = false;
        DebugLogger.LogDebug("[Round End Damage Immunity] Disabled.");
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        damageImmunity = true;
        DebugLogger.LogDebug("[Round End Damage Immunity] Enabled.");
        return HookResult.Continue;
    }
}