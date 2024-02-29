using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks; // Added for async support
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Added for JSON handling
using JsonException = Newtonsoft.Json.JsonException;

namespace STCustomModels
{
    [MinimumApiVersion(176)]
    public partial class STCustomModels : BasePlugin
    {
        public override string ModuleName => "STCustomModels";
        public override string ModuleVersion => $"1.1 - {AssemblyInfo.GetBuildTime()}";
        public override string ModuleAuthor => "heartbreakhotel (deafps)";
        public override string ModuleDescription => "A plugin for custom player models in SharpTimer";

        public const string ConfigFileName = "config.json";

        public string ModelDir = string.Empty;
        public string GameDir = string.Empty;

        public Config? Configuration;
        public static class AssemblyInfo
        {
            public static DateTime GetBuildTime()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileInfo = new System.IO.FileInfo(assembly.Location);
                return fileInfo.LastWriteTimeUtc;
            }
        }

        public override void Load(bool hotReload)
        {
            GameDir = Server.GameDirectory;
            LoadConfigAsync().Wait();

            ModelDir = GetModelsValue();

            var connectionString = $"Server={Configuration!.Database.HostName};Port={Configuration!.Database.Port};Database={Configuration!.Database.DataBase};Uid={Configuration!.Database.Username};Pwd={Configuration!.Database.Password};";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS `PlayerModels` (
                  `steamid` varchar(255) NOT NULL,
                  `model` varchar(255) NOT NULL
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";

                using (var createTableCommand = new MySqlCommand(createTableQuery, connection))
                {
                    createTableCommand.ExecuteNonQuery();
                }
            }

            RegisterEventHandler<EventPlayerSpawned>(async (@event, info) => 
            {
                if (@event.Userid == null) return HookResult.Continue;

                var player = @event.Userid;

                if (player.IsBot || !player.IsValid || player == null)
                {
                    return HookResult.Continue;
                }
                else
                {
                    Console.WriteLine(player.PlayerName);

                    var connectionString = $"Server={Configuration!.Database.HostName};Port={Configuration!.Database.Port};Database={Configuration!.Database.DataBase};Uid={Configuration!.Database.Username};Pwd={Configuration!.Database.Password};";

                    string? activemodel = null;

                    using (var connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync(); 

                        const string query = "SELECT model FROM PlayerModels WHERE SteamID = @SID";

                        using (var command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@SID", player.SteamID.ToString());

                            using (var reader = await command.ExecuteReaderAsync()) 
                            {
                                if (await reader.ReadAsync()) 
                                {
                                    activemodel = reader.GetString("model");
                                }
                            }
                        }
                    }

                    if (Configuration?.General.RequiresVIP ?? true)
                    {
                        var vipStatus = await GetVipStatusAsync(player);

                        if (vipStatus)
                        {

                            if (activemodel != null)
                            {
                                AddTimer(0.2f, () =>
                                {
                                    if (player.IsBot || !player.IsValid || player == null) return;
                                    player.Respawn();
                                    player.Pawn.Value.SetModel(activemodel);
                                    Console.WriteLine($"[STCustomModels] Model set to {ModelDir} for {player.PlayerName}");
                                });
                            }

                            return HookResult.Continue;
                        }
                    }
                    else
                    {
                        if (activemodel != null)
                        {
                            AddTimer(0.2f, () =>
                            {
                                if (player.IsBot || !player.IsValid || player == null) return;
                                player.Respawn();
                                player.Pawn.Value.SetModel(activemodel);
                                Console.WriteLine($"[STCustomModels] Model set to {ModelDir} for {player.PlayerName}");
                            });
                        }
                    }

                    return HookResult.Continue;
                }
            });

            Console.WriteLine("[STCustomModels] Plugin Loaded");
        }

        [ConsoleCommand("css_setmodel", "sets your model by index from cfg")]
        [CommandHelper(minArgs: 1, usage: "!setmodel [index]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public async void SetModel(CCSPlayerController? player, CommandInfo command) 
        {
            await LoadConfigAsync(); 

            if (Configuration?.General.RequiresVIP ?? true)
            {
                var vipStatus = await GetVipStatusAsync(player); 

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

                var connectionString = $"Server={Configuration!.Database.HostName};Port={Configuration!.Database.Port};Database={Configuration!.Database.DataBase};Uid={Configuration!.Database.Username};Pwd={Configuration!.Database.Password};";

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync(); 

                    const string query = "UPDATE PlayerModels SET model = @ModelPath WHERE SteamID = @SID";

                    using (var commandsql = new MySqlCommand(query, connection))
                    {
                        commandsql.Parameters.AddWithValue("@ModelPath", modelPath);
                        commandsql.Parameters.AddWithValue("@SID", player.SteamID.ToString());

                        int rowsAffected = await commandsql.ExecuteNonQueryAsync(); 

                        if (rowsAffected == 0)
                        {
                            const string insertQuery = "INSERT INTO PlayerModels (SteamID, model) VALUES (@SID, @ModelPath)";

                            using (var insertCommand = new MySqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@SID", player.SteamID.ToString());
                                insertCommand.Parameters.AddWithValue("@ModelPath", modelPath);

                                await insertCommand.ExecuteNonQueryAsync();
                            }

                            Console.WriteLine($"Inserted a new model record for SteamID: {player.SteamID}");
                        }
                    }
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
        public async void ListModels(CCSPlayerController? player, CommandInfo command) 
        {
            await LoadConfigAsync(); 

            if (Configuration?.General.RequiresVIP ?? true)
            {
                var vipStatus = await GetVipStatusAsync(player);

                if (!vipStatus)
                {
                    player.PrintToChat($" {ChatColors.Red}{Configuration!.General.ChatPrefix} - {ChatColors.Default}This function is limited to {ChatColors.Red}VIP{ChatColors.Default} users");
                    return;
                }
            }

            await PrintAllModels(player);
        }

        private async Task LoadConfigAsync() 
        {
            GameDir = Server.GameDirectory;
            var jsonPath = Path.Join(GameDir + "/csgo/addons/counterstrikesharp/plugins/STCustomModels", "config.json");

            using (var stream = File.OpenRead(jsonPath))
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync(); 
                Configuration = JsonConvert.DeserializeObject<Config>(json);

                if (Configuration == null)
                    throw new JsonException("Configuration could not be loaded");
            }
        }

        private async Task<bool> GetVipStatusAsync(CCSPlayerController? player) 
        {
            await LoadConfigAsync(); 

            var connectionString = $"Server={Configuration!.Database.HostName};Port={Configuration!.Database.Port};Database={Configuration!.Database.DataBase};Uid={Configuration!.Database.Username};Pwd={Configuration!.Database.Password};";

            var isVip = false;

            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync(); 

                const string query = "SELECT IsVip FROM PlayerStats WHERE SteamID = @SID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", player.SteamID.ToString());

                    using (var reader = await command.ExecuteReaderAsync()) 
                    {
                        if (await reader.ReadAsync()) 
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

        private async Task PrintAllModels(CCSPlayerController? player)
        {
            try
            {
                await LoadConfigAsync(); 

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
