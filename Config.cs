using Newtonsoft.Json;

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
}