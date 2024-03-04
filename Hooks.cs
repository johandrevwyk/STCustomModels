using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;
using static CounterStrikeSharp.API.Core.Listeners;

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

            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn == null || !pawn.IsValid)
                return HookResult.Continue;

            if (player.IsBot || !player.IsValid || player == null)
            {
                return HookResult.Continue;
            }
            else
            {
                AddTimer(2f, () => 
                {
                    if (Configuration?.General.RequiresVIP ?? true)
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
                                            player.Pawn.Value.SetModel(activemodel);
                                            Console.WriteLine($"[STCustomModels] Model set to {ModelDir} for {player.PlayerName}");

                                        });
                                    }
                                });
                            }
                            return HookResult.Continue;
                        });
                        
                    }
                    else
                    {
                        FetchModel(player.SteamID.ToString()).ContinueWith(modelTask =>
                        {
                            string activemodel = modelTask.Result;

                            if (activemodel != null)
                            {
                                AddTimer(2f, () =>
                                {
                                    Server.NextFrame(() =>
                                    {
                                        if (player.IsBot || !player.IsValid || player == null) return;
                                        //player.Respawn();
                                        player.Pawn.Value.SetModel(activemodel);
                                        Console.WriteLine($"[STCustomModels] Model set to {ModelDir} for {player.PlayerName}");

                                    });
                                });
                            }
                        });
                    }//
                });
            }
            return HookResult.Continue;
        }
    }
}
