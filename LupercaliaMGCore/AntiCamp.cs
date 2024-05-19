using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace LupercaliaMGCore {
    public class AntiCamp {
        private LupercaliaMGCore m_CSSPlugin;
        private CounterStrikeSharp.API.Modules.Timers.Timer timer;
        private Dictionary<CCSPlayerController, float> playerCampingTime = new Dictionary<CCSPlayerController, float>();
        private Dictionary<CCSPlayerController, PlayerPositionHistory> playerPositionHistory= new Dictionary<CCSPlayerController, PlayerPositionHistory>();
        private Dictionary<CCSPlayerController, float> playerGlowingTime = new Dictionary<CCSPlayerController, float>();
        private Dictionary<CCSPlayerController, bool> isPlayerWarned = new Dictionary<CCSPlayerController, bool>();
        private Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer> glowingTimer = new Dictionary<CCSPlayerController, CounterStrikeSharp.API.Modules.Timers.Timer>();

        private bool isRoundStarted = false;

        public AntiCamp(LupercaliaMGCore plugin, bool hotReload) {
            m_CSSPlugin = plugin;

            m_CSSPlugin.RegisterEventHandler<EventPlayerConnect>(onPlayerConnect, HookMode.Pre);
            m_CSSPlugin.RegisterEventHandler<EventPlayerConnectFull>(onPlayerConnectFull, HookMode.Pre);

            m_CSSPlugin.RegisterEventHandler<EventRoundFreezeEnd>(onRoundFeezeEnd, HookMode.Post);
            m_CSSPlugin.RegisterEventHandler<EventRoundEnd>(onRoundEnd, HookMode.Post);


            if(hotReload) {
                bool isFreezeTimeEnded = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules?.FreezePeriod ?? false;

                if(isFreezeTimeEnded) {
                    isRoundStarted = true;
                }

                foreach(var client in Utilities.GetPlayers()) {
                    if(!client.IsValid || client.IsBot || client.IsHLTV)
                        continue;

                    initClientInformation(client);
                }
            }

            timer = m_CSSPlugin.AddTimer(PluginSettings.getInstance.m_CVAntiCampDetectionInterval.Value, checkPlayerIsCamping, TimerFlags.REPEAT);
        }

        private void checkPlayerIsCamping() {
            if(!isRoundStarted)
                return;

            foreach(var client in Utilities.GetPlayers()) {
                if(!client.IsValid || client.IsBot || client.IsHLTV)
                    continue;

                if(!isClientInformationAccessible(client))
                    continue;

                Vector? clientOrigin = client.PlayerPawn.Value!.AbsOrigin;
                
                if(clientOrigin == null)
                    continue;

                playerPositionHistory[client].Update(new Vector(clientOrigin.X, clientOrigin.Y, clientOrigin.Z));

                TimedPosition? lastLocation = playerPositionHistory[client].GetOldestPosition();

                if(lastLocation == null)
                    continue;

                double distance = calculateDistance(lastLocation.vector, clientOrigin);

                if(distance <= PluginSettings.getInstance.m_CVAntiCampDetectionRadius.Value) {
                    playerCampingTime[client] += PluginSettings.getInstance.m_CVAntiCampDetectionInterval.Value;
                    // string msg = $"You have been camping for {playerCampingTime[client]:F2} | secondsGlowingTime: {playerGlowingTime[client]:F2} \nCurrent Location: {clientOrigin.X:F2} {clientOrigin.Y:F2} {clientOrigin.Z:F2} | Compared Location: {lastLocation.vector.X:F2} {lastLocation.vector.Y:F2} {lastLocation.vector.Z:F2} \nLocation captured time {lastLocation.time:F2} | Difference: {distance:F2}";
                    // client.PrintToCenter(msg);
                } else {
                    playerCampingTime[client] = 0.0F;
                }

                if(playerCampingTime[client] >= PluginSettings.getInstance.m_CVAntiCampDetectionTime.Value) {

                    if(playerGlowingTime[client] <= 0.0 && !isPlayerWarned[client]) {
                        startPlayerGlowing(client);
                        recreateGlowingTimer(client);
                    }

                    playerGlowingTime[client] = PluginSettings.getInstance.m_CVAntiCampMarkingTime.Value;
                }
            }
        }

        private HookResult onRoundFeezeEnd(EventRoundFreezeEnd @event, GameEventInfo info) {
            isRoundStarted = true;
            return HookResult.Continue;
        }

        private HookResult onRoundEnd(EventRoundEnd @event, GameEventInfo info) {
            isRoundStarted = false;
            return HookResult.Continue;
        }

        private HookResult onPlayerConnect(EventPlayerConnect @event, GameEventInfo info) {
            CCSPlayerController? client = @event.Userid;

            if(client == null) 
                return HookResult.Continue;

            if(!client.IsValid || client.IsBot || client.IsHLTV)
                return HookResult.Continue;

            initClientInformation(client);

            return HookResult.Continue;
        }

        private HookResult onPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info) {
            CCSPlayerController? client = @event.Userid;

            if(client == null) 
                return HookResult.Continue;

            if(!client.IsValid || client.IsBot || client.IsHLTV)
                return HookResult.Continue;
            
            if(isClientInformationAccessible(client))
                return HookResult.Continue;

            initClientInformation(client);
            return HookResult.Continue;
        }

        private bool isClientInformationAccessible(CCSPlayerController client) {
            return playerPositionHistory.ContainsKey(client) && playerCampingTime.ContainsKey(client) && playerGlowingTime.ContainsKey(client) && isPlayerWarned.ContainsKey(client);
        }

        private void initClientInformation(CCSPlayerController client) {
            playerPositionHistory[client] = new PlayerPositionHistory((int)(PluginSettings.getInstance.m_CVAntiCampDetectionTime.Value / PluginSettings.getInstance.m_CVAntiCampDetectionInterval.Value));
            playerCampingTime[client] = 0.0F;
            playerGlowingTime[client] = 0.0F;
            isPlayerWarned[client] = false;
        }

        private void recreateGlowingTimer(CCSPlayerController client) {
            float timerInterval = PluginSettings.getInstance.m_CVAntiCampDetectionInterval.Value;
            isPlayerWarned[client] = true;
            client.PrintToCenterAlert("You have detected as CAMPING. MOVE!");
            glowingTimer[client] = m_CSSPlugin.AddTimer(timerInterval, () => {
                if(playerGlowingTime[client] <= 0.0F) {
                    stopPlayerGlowing(client);
                    isPlayerWarned[client] = false;
                    glowingTimer[client].Kill();
                }
                playerGlowingTime[client] -= timerInterval;
            }, TimerFlags.REPEAT);
        }

        // TODO Glow player
        private void startPlayerGlowing(CCSPlayerController client) {
            playerGlowingTime[client] = 0.0F;
            CCSPlayerPawn playerPawn = client.PlayerPawn.Value!;
            playerPawn.Glow.GlowColorOverride = Color.Red;
            playerPawn.RenderMode = RenderMode_t.kRenderGlow;
            playerPawn.Glow.GlowRange = 6000;
            playerPawn.Glow.GlowTeam = -1;
            playerPawn.Glow.GlowType = 3;
            playerPawn.Glow.GlowRangeMin = 0;
        }

        // TODO Remove Glow player
        private void stopPlayerGlowing(CCSPlayerController client) {
            CCSPlayerPawn playerPawn = client.PlayerPawn.Value!;
            playerPawn.Glow.GlowColorOverride = Color.White;
            playerPawn.RenderMode = RenderMode_t.kRenderTransColor;
            playerPawn.Glow.GlowRange = 0;
            playerPawn.Glow.GlowTeam = 0;
            playerPawn.Glow.GlowType = 0;
        }


        private static double calculateDistance(Vector vec1, Vector vec2) {
            double deltaX = vec1.X - vec2.X;
            double deltaY = vec1.Y - vec2.Y;
            double deltaZ = vec1.Z - vec2.Z;

            double distanceSquared = deltaX * deltaX + deltaY * deltaY + deltaZ + deltaZ;
            return Math.Sqrt(distanceSquared);
        }

        private enum PlayerGlowStatus {
            GLOWING,
            NOT_GLOWING,
        }
    }
}