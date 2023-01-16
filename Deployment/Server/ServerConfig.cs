using SharedLib;

namespace Deployment.Server;

internal class ServerConfig
{
    public static ServerConfig Instance { get; private set; }
    private static string ConfigPath => Args.TryGetArg("-config", out var configPath)
        ? configPath 
        : "config-server.json";
    
    public bool RunServer { get; set; }
    public string IP { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 8080;
    public List<string> AuthTokens { get; set; }
    public SteamServerConfig Steam { get; set; }

    public static ServerConfig Load()
    {
        if (!File.Exists(ConfigPath))
            File.WriteAllText(ConfigPath, Json.Serialise(new ServerConfig()));

        var configStr = File.ReadAllText(ConfigPath);
        Instance = Json.Deserialise<ServerConfig>(configStr) ?? new ServerConfig();
        return Instance;
    }

    public void Refresh()
    {
        var newConfig = Load();
        AuthTokens = newConfig.AuthTokens;
    }
}