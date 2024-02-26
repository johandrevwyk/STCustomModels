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
    [MinimumApiVersion(165)]
    public partial class STCustomModels : BasePlugin
    {
        public override string ModuleName => "STCustomModels";
        public override string ModuleVersion => $"1.0 - {DateTime.UtcNow}";
        public override string ModuleAuthor => "heartbreakhotel (deafps)";
        public override string ModuleDescription => "A plugin for custom player models in SharpTimer";

        public const string ConfigFileName = "config.json";

        public string ModelDir = string.Empty;
        public string GameDir = string.Empty;

        public Config? Configuration;
        
        public override void Load(bool hotReload)
        {
            GameDir = Server.GameDirectory;
            LoadConfig();
            
            ModelDir = GetModelsValue();   
            
            Console.WriteLine("[STCustomModels] Plugin Loaded");
        }

        [ConsoleCommand("css_setmodel", "sets your model by index from cfg")]
        [CommandHelper(minArgs: 1, usage: "!setmodel [index]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetModel(CCSPlayerController? player, CommandInfo command)
        {
            if (Configuration?.General.RequiresVIP ?? true)
            {
                var vipStatus = GetVipStatus(player);

                if (!vipStatus)
                {
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users");
                    return;
                }
            }

            if (int.TryParse(command.GetArg(1), out var index))
            {
                var modelPath = GetModelsValue(index);

                if (string.IsNullOrEmpty(modelPath))
                {
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default} Incorrect model");
                    return;
                }

                AddTimer(0.2f, () =>
                {
                    if (player.IsBot || !player.IsValid || player == null) return;
                    player.Respawn();
                    player.Pawn.Value.SetModel(modelPath);
                    Console.WriteLine($"[STCustomModels] Model set to {modelPath} for {player.PlayerName} from chat command");
                    string modelName = Path.GetFileNameWithoutExtension(modelPath);
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}Model set to: {ChatColors.Red}{modelName}");
                });
            }
            else
            {
                player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}Incorrect model");
            }
        }


        [ConsoleCommand("css_models", "lists all models")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ListModels(CCSPlayerController? player, CommandInfo command)
        {
            // Check the configuration to see if VIP check is required
            if (Configuration?.General.RequiresVIP ?? true)
            {
                var vipStatus = GetVipStatus(player);
              
                if (!vipStatus) 
                {
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users");
                    return;
                }
            }

            
            PrintAllModels(player);
        }

        private void PrintAllModels(CCSPlayerController? player)
        {
            try
            {
                
                var models = Configuration!.Models;

                var index = 0;
                foreach (var model in models)
                {
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} -{ChatColors.Default} #{index++}: {model.Name}");
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
        private bool GetVipStatus(CCSPlayerController? player)
        {           
            var connectionString = $"Server={Configuration!.Database.HostName};Port={Configuration!.Database.Port};Database={Configuration!.Database.DataBase};Uid={Configuration!.Database.Username};Pwd={Configuration!.Database.Password};";

            var isVip = false;
            
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                const string query = "SELECT IsVip FROM PlayerStats WHERE SteamID = @SID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", player.SteamID.ToString());

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isVip = reader.GetBoolean("IsVip");
                        }
                    }
                }
            }

            return isVip;
        }

        private string GetModelsValue(int index = 0)
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
