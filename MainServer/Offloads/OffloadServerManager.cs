using MainServer.Configs;

namespace MainServer.Offloads;

internal static class OffloadServerManager
{
    private static readonly Dictionary<string, OffloadServer> _offloadServers = new();
    public static IEnumerable<OffloadServer> All => _offloadServers.Values;

    public static async void Init(List<OffloadServerConfig>? configRunners)
    {
        if (configRunners is null)
            return;

        foreach (var runner in configRunners)
        {
            var offloadServer = new OffloadServer(runner.Ip, runner.Port);
            _offloadServers.Add(runner.Id, offloadServer);
        }
    }

    public static OffloadServer GetOffloadServerWithId(string id)
    {
        if (_offloadServers.TryGetValue(id, out var offloadServer))
            return offloadServer;

        throw new NullReferenceException($"Offload server not found: {id}");
    }
    
    public static OffloadServer GetOffloadServer(string operatingSystem)
    {
        return _offloadServers.Values.FirstOrDefault(x => x.OperatingSystem == operatingSystem)
               ?? throw new NullReferenceException($"Server not found with operating system: {operatingSystem}");
    }
}