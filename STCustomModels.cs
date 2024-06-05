using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace STCustomModels
{
    public partial class STCustomModels : BasePlugin, IPluginConfig<ModelConfig>
    {
        public override string ModuleName => "STCustomModels";
        public override string ModuleVersion => $"1.2";
        public override string ModuleAuthor => "heartbreakhotel";
        public override string ModuleDescription => "A plugin for custom player models in SharpTimer";

        
        public override void Load(bool hotReload)
        {
            RegisterListeners();

            _ = CreatePlayerModelsTableIfNotExists();


            if (Config.General.PrecacheModels)
            {
                RegisterListener<Listeners.OnServerPrecacheResources>((manifest) =>
                {
                    foreach (var model in Config.Models)
                    {
                        manifest.AddResource(model);
                    }
                });
            }

            Console.WriteLine("[STCustomModels] Plugin Loaded");
        }

        public void PrintAllModels(CCSPlayerController? player)
        {
            if (player == null) return;
            var models = Config.Models;

            var index = 0;
            foreach (var model in models)
            {
                Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Config.General.ChatPrefix} -{ChatColors.Default} #{index++}: {model}"));
            }
        }

        public async Task SetModel(CCSPlayerController? player, string arg, string steamid)
        {
            if (player == null) return;

            if (Config.General.RequiresVIP)
            {
                if (await GetVipStatusAsync(steamid) == false)
                {
                    Server.NextFrame(() => player.PrintToChat($"{ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users"));
                    return;
                }
            }

            if (int.TryParse(arg, out var index))
            {
                if (index < 0 || index >= Config.Models.Length)
                {
                    Server.NextFrame(() => player.PrintToChat($"{ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}Invalid model index"));
                    return;
                }

                var modelPath = Config.Models[index];
                Console.WriteLine(modelPath);

                if (string.IsNullOrEmpty(modelPath))
                {
                    Server.NextFrame(() => player.PrintToChat($"{ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}Incorrect model"));
                    return;
                }

                Server.NextFrame(() =>
                {
                    if (player.IsBot || !player.IsValid || player == null) return;
                    player.Pawn.Value!.SetModel(modelPath);
                    Console.WriteLine($"[STCustomModels] Model set to {modelPath} for {player.PlayerName} from chat command");
                    player.PrintToChat($"{ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}Model set to: {ChatColors.Red}{modelPath}");
                });

                bool recordExists = await CheckIfRecordExists(steamid);

                if (recordExists)
                    await UpdateModel(steamid, modelPath);
                else
                    await InsertModel(steamid, modelPath);
            }
            else
            {
                Server.NextFrame(() => player.PrintToChat($"{ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}Incorrect model"));
            }
        }


        private async Task ListModels(CCSPlayerController? player)
        {
            if (player == null) return;
            if (Config.General.RequiresVIP == true)
            {
                if (await GetVipStatusAsync(player.SteamID.ToString()) == false)
                {
                    Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Config.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users"));
                    return;
                }
            }

            PrintAllModels(player);
        }
    }
}
