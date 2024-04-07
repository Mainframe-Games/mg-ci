using Newtonsoft.Json.Linq;

namespace MainServer.Configs;

public class ServerConfig
{
    public string Ip { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 8080;
    public UnityServicesConfig? Ugs { get; set; }
    public SteamServerConfig? Steam { get; set; }
    public AmazonS3Config? S3 { get; set; }
    public ClanforgeConfig? Clanforge { get; set; }
    public XcodeConfig? AppleStore { get; set; }
    public GooglePlayConfig? GoogleStore { get; set; }
    public GitConfig? Git { get; set; }

    public List<BuildRunnerConfig>? Runners { get; set; }

    public override string ToString()
    {
        return JObject.FromObject(this).ToString();
    }
}

public class BuildRunnerConfig
{
    public string? Id { get; set; }
    public string? Ip { get; set; }
    public ushort Port { get; set; }
}
