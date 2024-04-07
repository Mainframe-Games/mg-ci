using System.Net.Http.Headers;
using MainServer.Configs;
using MainServer.Services.Client;
using SocketServer;
using SocketServer.Messages;

namespace MainServer;

internal static class ClientServicesManager
{
    private static readonly Dictionary<string, BuildRunnerClientService> _buildRunners = new();
    public static IEnumerable<BuildRunnerClientService> All => _buildRunners.Values;

    private static List<Client> _clients = [];

    public static void Init(List<BuildRunnerConfig>? configRunners)
    {
        if (configRunners is null)
            return;

        foreach (var runner in configRunners)
        {
            var client = new Client(runner.Ip!, runner.Port);
            _clients.Add(client);

            client.AddService(new BuildClientService(client));
            client.AddService(new XcodeClientService(client));
            var runnerService = new BuildRunnerClientService(client);
            client.AddService(runnerService);
            _buildRunners.Add(runner.Id!, runnerService);
        }
    }

    public static XcodeClientService GetXcodeService()
    {
        foreach (var client in _clients)
        {
            if (client.TryGetService("xcode", out var xcodeService))
                return (XcodeClientService)xcodeService!;
        }

        throw new Exception("xcode service not found");
    }

    public static BuildRunnerClientService GetDefaultRunner()
    {
        return _buildRunners.Values.First();
    }

    public static BuildRunnerClientService GetRunner(OperationSystemType operatingSystem)
    {
        return _buildRunners.Values.FirstOrDefault(x => x.ServerOperationSystem == operatingSystem)
            ?? throw new NullReferenceException(
                $"Server not found with operating system: {operatingSystem}"
            );
    }
}
