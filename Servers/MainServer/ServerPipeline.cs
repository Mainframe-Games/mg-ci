using System.Diagnostics;
using MainServer.Services.Client;
using MainServer.Services.Packets;
using MainServer.Workspaces;
using SocketServer.Messages;

namespace MainServer;

internal class ServerPipeline(
    Guid projectGuid,
    Workspace workspace,
    IEnumerable<string> buildTargets
)
{
    public static List<Guid> ActiveProjects { get; } = [];

    public async void Run()
    {
        ActiveProjects.Add(projectGuid);
        {
            var sw = Stopwatch.StartNew();

            // version bump
            var fullVersion = workspace.VersionBump();
            Console.WriteLine($"Pre Build Complete\ntime = {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // changelog
            var changeLog = workspace.GetChangeLog();

            // builds
            var processes = await RunBuildAsync();
            Console.WriteLine($"Build Complete\ntime= {sw.ElapsedMilliseconds}ms");
            sw.Restart();

            // deploys
            RunDeploy(processes, fullVersion, changeLog);
            Console.WriteLine($"Deploy Complete\ntime= {sw.ElapsedMilliseconds}ms");
            sw.Restart();
        }
        ActiveProjects.Remove(projectGuid);
    }

    #region Build

    private async Task<List<BuildRunnerProcess>> RunBuildAsync()
    {
        var buildProcesses = new List<BuildRunnerProcess>();
        var tasks = new List<Task>();

        // assign callbacks
        foreach (var runner in BuildRunnerManager.All)
        {
            runner.OnBuildCompleteMessage += (targetName, buildTime) =>
            {
                foreach (var process in buildProcesses)
                    process.OnStringMessage(targetName, buildTime);
            };
        }

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

        // clear callbacks
        // foreach (var runner in BuildRunnerFactory.All)
        //     runner.ClearEvents();

        return buildProcesses;
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
        // var deployRunner = new DeploymentRunner(project, workspace, fullVersion, changeLog);
        // deployRunner.Deploy();
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
