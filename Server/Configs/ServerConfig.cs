using Deployment.Configs;
using SharedLib;
using Tomlyn;

namespace Server.Configs;

public class ServerConfig
{
    public static ServerConfig Instance { get; private set; } = Load();
    private static string ConfigPath =>
        Args.Environment.TryGetArg("-config", out var configPath)
            ? configPath
            : "config-server.toml";

    public string IP { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 8080;
    public List<string>? AuthTokens { get; set; }
    public OffloadConfig? Offload { get; set; }
    public HooksConfig[]? Hooks { get; set; }
    public UnityServicesConfig? Ugs { get; set; }
    public SteamServerConfig? Steam { get; set; }
    public AmazonS3Config? S3 { get; set; }
    public ClanforgeConfig? Clanforge { get; set; }
    public XcodeConfig? AppleStore { get; set; }
    public GooglePlayConfig? GoogleStore { get; set; }
    public GitConfig? Git { get; set; }

    public static ServerConfig Load()
    {
        var path = ConfigPath;

        if (!File.Exists(path))
            File.WriteAllText(path, Toml.FromModel(new ServerConfig()));

        var configStr = File.ReadAllText(path);
        Instance = Toml.ToModel<ServerConfig>(configStr);
        return Instance;
    }

    public void Refresh()
    {
        var newConfig = Load();
        AuthTokens = newConfig.AuthTokens;
    }

    public override string ToString()
    {
        return Json.Serialise(this);
    }
}

public class OffloadConfig
{
    public string? Url { get; set; }
    public List<BuildTargetFlag>? Targets { get; set; }
}
