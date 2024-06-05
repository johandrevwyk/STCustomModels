/*using Newtonsoft.Json;

namespace STCustomModels;

public class Config
{
    public General General { get; set; }
    public Database Database { get; set; }
    public List<Model> Models { get; set; }
}

public class General
{
    public string ChatPrefix { get; set; }
    public bool RequiresVIP { get; set; }
}

public class Database
{
    [JsonProperty("hostname")]
    public string HostName { get; set; }
    
    [JsonProperty("port")]
    public string Port { get; set; }
    
    [JsonProperty("username")]
    public string Username { get; set; }
    
    [JsonProperty("password")]
    public string Password { get; set; }
    
    [JsonProperty("database")]
    public string DataBase { get; set; }
}

public class Model
{
    public string Name { get; set; }
    
    [JsonProperty("Model")]
    public string ModelPath { get; set; }
}*/

namespace STCustomModels
{
    using CounterStrikeSharp.API.Core;
    using Newtonsoft.Json;
    using System.Reflection.Metadata;
    using System.Text.Json.Serialization;

    public class General
    {
        [JsonPropertyName("ChatPrefix")]
        public string ChatPrefix { get; set; } = "[SERVER]";

        [JsonPropertyName("RequiresVIP")]
        public bool RequiresVIP { get; set; } = true;

        [JsonPropertyName("PrecacheModels")]
        public bool PrecacheModels { get; set; } = true;
    }

    public class Database
    {
        [JsonProperty("hostname")]
        public string HostName { get; set; } = "hostname";

        [JsonProperty("port")]
        public string Port { get; set; } = "3306";

        [JsonProperty("username")]
        public string Username { get; set; } = "root";

        [JsonProperty("password")]
        public string Password { get; set; } = "";

        [JsonProperty("database")]
        public string DataBase { get; set; } = "";
    }

    public class ModelConfig : BasePluginConfig
    {
        [JsonPropertyName("General")]
        public General General { get; set; } = new General();

        [JsonPropertyName("Database")]
        public Database Database { get; set; } = new Database();

        [JsonPropertyName("Models")]
        public string[] Models { get; set; } = { "example.vmdl", "example.vmdl" };

        [JsonPropertyName("ConfigVersion")]
        public override int Version { get; set; } = 1;
    }

    public partial class STCustomModels : BasePlugin, IPluginConfig<ModelConfig>
    {
        public required ModelConfig Config { get; set; }

        public void OnConfigParsed(ModelConfig config)
        {
            Console.WriteLine("Config Loaded Succesfully");
            Config = config;

            if (string.IsNullOrEmpty(Config.Database.DataBase)) throw new Exception("Configure the database settings");
            if (Config.Models.Length == 0) throw new Exception("Empty model list");
            if (Config.Version != 1) throw new Exception("Config version mismatch");

        }
    }

}