using SharedLib;

namespace Deployment.Server;

internal class ServerConfig
{
    private const string CONFIG = "config-server.json";
    public static ServerConfig Instance { get; private set; }
    
    public bool RunServer { get; set; }
    public string IP { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 8080;
    public List<string> AuthTokens { get; set; }
    public string? SteamPath { get; set; }

    public static ServerConfig Load()
    {
        if (!File.Exists(CONFIG))
            File.WriteAllText(CONFIG, Json.Serialise(new ServerConfig()));

        var configStr = File.ReadAllText(CONFIG);
        Instance = Json.Deserialise<ServerConfig>(configStr) ?? new ServerConfig();
        return Instance;
    }

    public void Refresh()
    {
        var newConfig = Load();
        if (newConfig == null)
            return;

        AuthTokens = newConfig.AuthTokens;
    }
}