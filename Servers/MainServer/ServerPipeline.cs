using System.Diagnostics;
using MainServer.Configs;
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
            var processes = await RunBuildAsync();
            Console.WriteLine($"Build Complete\n  time: {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // deploys
            RunDeploy(processes, fullVersion, changeLog);
            Console.WriteLine($"Deploy Complete\n  time: {sw.ElapsedMilliseconds}ms");
            sw.Restart();
        }
        ActiveProjects.Remove(projectGuid);
        BuildRunnerClientService.OnBuildCompleteMessage -= OnBuildCompleted;
    }

    #region Build

    private async Task<List<BuildRunnerProcess>> RunBuildAsync()
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

        return buildProcesses;
    }

    private void OnBuildCompleted(string targetName, long buildTime)
    {
        foreach (var process in buildProcesses)
            process.OnStringMessage(targetName, buildTime);
    }

    private static BuildRunnerClientService GetUnityRunner(string targetName, bool isIL2CPP)
    {
        if (!isIL2CPP)
            return BuildRunnerManager.GetDefaultRunner();
        if (targetName.Contains("Windows"))
            return BuildRunnerManager.GetRunner(OperationSystemType.Windows);
        if (targetName.Contains("OSX"))
            return BuildRunnerManager.GetRunner(OperationSystemType.MacOS);
        if (targetName.Contains("Linux"))
            return BuildRunnerManager.GetRunner(OperationSystemType.Linux);

        throw new NotSupportedException($"Target not supported: {targetName}");
    }

    #endregion

    #region Deploy

    private void RunDeploy(
        List<BuildRunnerProcess> buildProcesses,
        string fullVersion,
        string[] changeLog
    )
    {
        var deployRunner = new DeploymentRunner(workspace, fullVersion, changeLog, serverConfig);
        deployRunner.Deploy();
    }

    #endregion

    private class BuildRunnerProcess
    {
        private readonly TaskCompletionSource _completionSource = new();
        private readonly string _buildName;

        public Task Task => _completionSource.Task;

        public long TotalBuildTime { get; private set; }

        public BuildRunnerProcess(string buildName)
        {
            _buildName = buildName;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var cacheRoot = new DirectoryInfo(Path.Combine(home, "ci-cache"));
            var rootPath = Path.Combine(cacheRoot.FullName, "Deploy", _buildName);
            var rootDir = new DirectoryInfo(rootPath);
            if (rootDir.Exists)
                rootDir.Delete(true);
        }

        public void OnStringMessage(string targetName, long buildTime)
        {
            if (targetName != _buildName)
                return;

            TotalBuildTime = buildTime;
            _completionSource.SetResult();
        }
    }
}
