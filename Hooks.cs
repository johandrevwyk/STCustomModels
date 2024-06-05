using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace STCustomModels
{
    public partial class STCustomModels
    {
        private void RegisterListeners()
        {
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (player == null) return HookResult.Continue;
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn == null || !pawn.IsValid)
                return HookResult.Continue;

            if (player.IsBot || !player.IsValid || player == null)
            {
                return HookResult.Continue;
            }
            else
            {
                AddTimer(0.5f, () =>
                {
                    if (Config.General.RequiresVIP == true)
                    {
                        GetVipStatusAsync(player.SteamID.ToString()).ContinueWith(vipTask =>
                        {
                            if (vipTask.Result == true)
                            {
                                FetchModel(player.SteamID.ToString()).ContinueWith(modelTask =>
                                {
                                    string activemodel = modelTask.Result;

                                    if (activemodel != null)
                                    {
                                        Server.NextFrame(() =>
                                        {
                                            if (player.IsBot || !player.IsValid || player == null) return;
                                            //player.Respawn();
                                            player.Pawn.Value!.SetModel(activemodel);
                                            player.PrintToChat($" {ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}VIP model set to: {ChatColors.Red}{activemodel}");
                                            Console.WriteLine($"[STCustomModels] Model set to {activemodel} for {player.PlayerName}");                                         
                                        });
                                    }
                                });
                            }
                            return HookResult.Handled;
                        });

                    }
                    else
                    {
                        FetchModel(player.SteamID.ToString()).ContinueWith(modelTask =>
                        {
                            string activemodel = modelTask.Result;

                            if (activemodel != null)
                            {
                                Server.NextFrame(() =>
                                {
                                    if (player.IsBot || !player.IsValid || player == null) return;
                                    player.Pawn.Value!.SetModel(activemodel);
                                    player.PrintToChat($" {ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}Custom model set to: {ChatColors.Red}{activemodel}");
                                    Console.WriteLine($"[STCustomModels] Model set to {activemodel} for {player.PlayerName}");                                  
                                });
                            }
                        });
                    }
                });
            }
            return HookResult.Handled;
        }
    }
}