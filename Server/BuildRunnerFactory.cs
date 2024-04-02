using Server.Configs;
using ServerClientShared;

namespace Server;

public static class BuildRunnerFactory
{
    private static readonly Dictionary<string, WebClient> _runners = new();
    public static readonly WebClient VersionBump = new("version-bump", "127.0.0.1", 8081);

    public static IEnumerable<WebClient> All => _runners.Values;

    public static async void Init(List<BuildRunnerConfig>? configRunners)
    {
        await VersionBump.Connect();

        if (configRunners is null)
            return;

        foreach (var runner in configRunners)
            ConnectToRunner(runner.Id, runner.Ip, runner.Port);
    }

    private static async void ConnectToRunner(string? id, string? ip, ushort port)
    {
        if (string.IsNullOrEmpty(id))
            throw new NullReferenceException("parameter 'id' is null or empty");

        var runner = new WebClient("build", ip, port);
        await runner.Connect();
        _runners.Add(id, runner);
    }

    public static WebClient GetRunner(string? id = null)
    {
        if (_runners.Count == 1)
            return _runners.First().Value;

        return _runners[id ?? string.Empty];
    }
}
