using MainServer.Configs;
using MainServer.Services.Client;
using SocketServer;
using SocketServer.Messages;

namespace MainServer;

internal static class BuildRunnerManager
{
    private static readonly Dictionary<string, BuildRunnerClientService> _buildRunners = new();
    public static IEnumerable<BuildRunnerClientService> All => _buildRunners.Values;

    public static void Init(List<BuildRunnerConfig>? configRunners)
    {
        if (configRunners is null)
            return;

        foreach (var runner in configRunners)
        {
            var client = new Client(runner.Ip!, runner.Port);
            client.AddService(new BuildClientService(client));
            var runnerService = new BuildRunnerClientService(client);
            client.AddService(runnerService);
            _buildRunners.Add(runner.Id!, runnerService);
        }
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
