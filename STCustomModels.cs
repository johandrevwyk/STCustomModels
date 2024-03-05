using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using Newtonsoft.Json;
using JsonException = Newtonsoft.Json.JsonException;

namespace STCustomModels
{
    [MinimumApiVersion(188)]
    public partial class STCustomModels : BasePlugin
    {
        public override string ModuleName => "STCustomModels";
        public override string ModuleVersion => $"1.1 - {new DateTime(Builtin.CompileTime, DateTimeKind.Utc)}"; // im too lazy to fix this
        public override string ModuleAuthor => "heartbreakhotel (deafps)";
        public override string ModuleDescription => "A plugin for custom player models in SharpTimer";

        public const string ConfigFileName = "config.json";

        public string ModelDir = string.Empty;
        public string GameDir = string.Empty;

        public Config? Configuration;
        
        public override void Load(bool hotReload)
        {
            GameDir = Server.GameDirectory;

            RegisterListeners();
            
            
            //Initialize config and database
            LoadConfig();
            CreatePlayerModelsTableIfNotExists();



            Console.WriteLine("[STCustomModels] Plugin Loaded");
        }

        public async Task PrintAllModels(CCSPlayerController? player)
        {
            try
            {
                ModelDir = await GetModelsValue();
                var models = Configuration!.Models;


                var index = 0;
                foreach (var model in models)
                {
                    Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} -{ChatColors.Default} #{index++}: {model.Name}"));
                }
                
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("JSON file not found.");
            }
            catch (JsonException)
            {
                Console.WriteLine("Error parsing JSON.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public async Task SetModel(CCSPlayerController? player, string arg, string steamid)
        {
            if (Configuration?.General.RequiresVIP ?? true)
            {
                if (await GetVipStatusAsync(steamid) == false)
                {
                    Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users"));
                    return;
                }
            }

            if (int.TryParse(arg, out var index))
            {
                var modelPath = await GetModelsValue(index);
                Console.WriteLine(modelPath);
                if (string.IsNullOrEmpty(modelPath))
                {
                    Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default} Incorrect model"));
                    return;
                }

                Server.NextFrame(() =>
                {
                    if (player.IsBot || !player.IsValid || player == null) return;
                    Console.WriteLine("respawning");
                    //player.Respawn();
                    player.Pawn.Value.SetModel(modelPath);
                    Console.WriteLine($"[STCustomModels] Model set to {modelPath} for {player.PlayerName} from chat command");
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}Model set to: {ChatColors.Red}{modelPath}");
                });

                bool recordExists = await CheckIfRecordExists(steamid);

                if (recordExists)
                    await UpdateModel(steamid, modelPath);
                else
                    await InsertModel(steamid, modelPath);
            }
            else
            {
                Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}Incorrect model"));
            }
        }

        private async Task ListModels(CCSPlayerController? player)
        {
            if (Configuration?.General.RequiresVIP ?? true)
            {
                if (await GetVipStatusAsync(player.SteamID.ToString()) == false)
                {
                    Server.NextFrame(() => player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users"));
                    return;
                }
            }

            await PrintAllModels(player);
        }

        public void IDontEvenKnow(CCSPlayerController? player)
        {

        }

        public async Task <string?> GetModelsValue(int index = 0)
        {
            try
            {
                var models = Configuration!.Models;

                if (models.Count > index)
                {
                    var model = models[index];

                    var modelName = model.Name;
                    var modelPath = model.ModelPath;
                    
                    return modelPath;
                }
                else
                {
                    Console.WriteLine("Index out of range.");
                    return null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }

        private void LoadConfig()
        {
            GameDir = Server.GameDirectory;
            var jsonPath = Path.Join(GameDir + "/csgo/addons/counterstrikesharp/plugins/STCustomModels", "config.json");

            Configuration = JsonConvert.DeserializeObject<Config>(File.ReadAllText(jsonPath));

            if (Configuration == null)
                throw new JsonException("Configuration could not be loaded");
        }
    }
}
