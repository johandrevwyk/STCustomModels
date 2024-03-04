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
            var connection = new MySqlConnection($"Server={Configuration!.Database.HostName};Port={Configuration!.Database.Port};Database={Configuration!.Database.DataBase};Uid={Configuration!.Database.Username};Pwd={Configuration!.Database.Password};");
            await connection.OpenAsync();
            return connection;
        }

        public async Task CreatePlayerModelsTableIfNotExists()
        {
            using (var connection = await OpenDatabaseConnectionAsync())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS `PlayerModels` (" +
                                          "`steamid` varchar(255) NOT NULL," +
                                          "`model` varchar(255) NOT NULL," +
                                          ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
                    await command.ExecuteNonQueryAsync();
                }
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
