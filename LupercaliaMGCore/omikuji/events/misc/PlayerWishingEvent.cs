using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace LupercaliaMGCore {
    public static partial class OmikujiEvents {
        [OmikujiFunc("Player Wishing Event", OmikujiType.EVENT_MISC)]
        
        public static void playerPlayEvent(CCSPlayerController client) {
            LupercaliaMGCore.getInstance().Logger.LogDebug("Player drew a omikuji and invoked Player wishing event");
            foreach(CCSPlayerController cl in Utilities.GetPlayers()) {
                if(!cl.IsValid || cl.IsBot || cl.IsHLTV)
                    continue;

                cl.PrintToChat($"{Omikuji.CHAT_PREFIX} {client.PlayerName} is wishing your good luck!");
            }
        }
    }
}