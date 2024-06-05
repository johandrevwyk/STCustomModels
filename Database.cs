using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;

namespace STCustomModels
{
    partial class STCustomModels
    {
        private async Task<MySqlConnection> OpenDatabaseConnectionAsync()
        {
            var connectionString = $"Server={Config.Database.HostName};Port={Config.Database.Port};Database={Config.Database.DataBase};Uid={Config.Database.Username};Pwd={Config.Database.Password};";
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task CreatePlayerModelsTableIfNotExists()
        {
            try
            {
                using var connection = await OpenDatabaseConnectionAsync();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE IF NOT EXISTS `PlayerModels` (" +
                                      "`steamid` varchar(255) NOT NULL," +
                                      "`model` varchar(255) NOT NULL," +
                                      "PRIMARY KEY (`steamid`))";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                Console.WriteLine($"An error occurred while creating the table: {ex.Message}");
            }
        }

        private async Task<bool> GetVipStatusAsync(string steamid)
        {
            var isVip = false;

            using (var connection = await OpenDatabaseConnectionAsync())
            {
                const string query = "SELECT IsVip FROM PlayerStats WHERE SteamID = @SID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", steamid);

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

        private async Task UpdateModel(string steamid, string modelPath)
        {
            using (var connection = await OpenDatabaseConnectionAsync())
            {
                const string query = "UPDATE PlayerModels SET model = @model WHERE steamid = @SID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", steamid);
                    command.Parameters.AddWithValue("@model", modelPath);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task InsertModel(string steamid, string modelPath)
        {
            using (var connection = await OpenDatabaseConnectionAsync())
            {
                const string query = "INSERT INTO PlayerModels (steamid, model) VALUES (@SID, @model) ON DUPLICATE KEY UPDATE model = @model";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", steamid);
                    command.Parameters.AddWithValue("@model", modelPath);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        private async Task<bool> CheckIfRecordExists(string steamid)
        {
            using (var connection = await OpenDatabaseConnectionAsync())
            {
                const string query = "SELECT COUNT(*) FROM PlayerModels WHERE steamid = @SID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", steamid);

                    var count = await command.ExecuteScalarAsync();
                    return (long)count > 0;
                }
            }
        }

        private async Task<string> FetchModel(string steamid)
        {
            string? activemodel = null;

            using (var connection = await OpenDatabaseConnectionAsync())
            {
                const string query = "SELECT model FROM PlayerModels WHERE SteamID = @SID";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SID", steamid);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            activemodel = reader.GetString("model");
                        }
                    }
                }
            }

            return activemodel;
        }
    }
}
