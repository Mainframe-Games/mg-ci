using System.Diagnostics;
using MainServer.Configs;
using MainServer.Deployment;
using MainServer.Hooks;
using MainServer.Services.Client;
using MainServer.Services.Packets;
using MainServer.Workspaces;
using SocketServer.Messages;

namespace MainServer;

internal class ServerPipeline(
    Guid projectGuid,
    Workspace workspace,
    IEnumerable<string> buildTargets,
    ServerConfig serverConfig
)
{
    public static List<Guid> ActiveProjects { get; } = [];
    private readonly List<BuildRunnerProcess> buildProcesses = [];

    public async void Run()
    {
        BuildRunnerClientService.OnBuildCompleteMessage += OnBuildCompleted;
        ActiveProjects.Add(projectGuid);
        {
            var sw = Stopwatch.StartNew();

            // version bump
            var fullVersion = workspace.VersionBump();
            Console.WriteLine($"Pre Build Complete\n  time: {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // changelog
            var changeLog = workspace.GetChangeLog();

            // builds
            await RunBuildAsync();
            Console.WriteLine($"Build Complete\n  time: {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // deploys
            await RunDeploy(fullVersion, changeLog);
            Console.WriteLine($"Deploy Complete\n  time: {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // hooks
            var buildResults = buildProcesses
                .Select(p => (p.BuildName, TimeSpan.FromMilliseconds(p.TotalBuildTime)))
                .ToList();
            var hookRunner = new HooksRunner(
                workspace,
                TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                buildResults,
                changeLog,
                fullVersion
            );
            hookRunner.Run();

            // apply tag
            workspace.Tag(fullVersion);
        }
        ActiveProjects.Remove(projectGuid);
        BuildRunnerClientService.OnBuildCompleteMessage -= OnBuildCompleted;
    }

    #region Build

    private async Task RunBuildAsync()
    {
        var tasks = new List<Task>();

        // start processes
        foreach (var buildTarget in buildTargets)
        {
            var runner = GetUnityRunner(buildTarget, false);
            var packet = new BuildRunnerPacket
            {
                ProjectGuid = projectGuid,
                TargetName = buildTarget,
                Branch = workspace.Branch,
            };
            runner.SendJson(packet.ToJson());

            var process = new BuildRunnerProcess(buildTarget);
            buildProcesses.Add(process);
            tasks.Add(process.Task);
        }

        // wait for them all to finish
        await Task.WhenAll(tasks);
    }

    private void OnBuildCompleted(string targetName, long buildTime)
    {
        foreach (var process in buildProcesses)
            process.OnStringMessage(targetName, buildTime);
    }

    private static BuildRunnerClientService GetUnityRunner(string targetName, bool isIL2CPP)
    {
        if (!isIL2CPP)
            return ClientServicesManager.GetDefaultRunner();
        if (targetName.Contains("Windows"))
            return ClientServicesManager.GetRunner(OperationSystemType.Windows);
        if (targetName.Contains("OSX"))
            return ClientServicesManager.GetRunner(OperationSystemType.MacOS);
        if (targetName.Contains("Linux"))
            return ClientServicesManager.GetRunner(OperationSystemType.Linux);

        throw new NotSupportedException($"Target not supported: {targetName}");
    }

    #endregion

    #region Deploy

    private async Task RunDeploy(string fullVersion, string[] changeLog)
    {
        var deployRunner = new DeploymentRunner(workspace, fullVersion, changeLog, serverConfig);
        await deployRunner.Deploy();
    }

    #endregion

    private class BuildRunnerProcess(string buildName)
    {
        private readonly TaskCompletionSource _completionSource = new();
        public readonly string BuildName = buildName;

        public Task Task => _completionSource.Task;

        public long TotalBuildTime { get; private set; }

        public void OnStringMessage(string targetName, long buildTime)
        {
            if (targetName != BuildName)
                return;

            TotalBuildTime = buildTime;
            _completionSource.SetResult();
        }
    }
}
