using System.Diagnostics;
using MainServer.Services.Client;
using MainServer.Workspaces;
using Newtonsoft.Json.Linq;

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
            runner.OnDataMessageReceived += bytes =>
            {
                foreach (var process in buildProcesses)
                    process.OnDataReceived(bytes);
            };
        }

        // start processes
        foreach (var buildTarget in buildTargets)
        {
            var runner = GetUnityRunner(buildTarget);
            await runner.SendJson(
                new JObject
                {
                    ["ProjectGuid"] = projectGuid,
                    ["TargetName"] = buildTarget,
                    ["Branch"] = workspace.Branch
                }
            );

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

    private static BuildRunnerClientService GetUnityRunner(string targetName)
    {
        if (targetName.Contains("Windows"))
            return BuildRunnerManager.GetOffloadServer("windows");
        if (targetName.Contains("OSX"))
            return BuildRunnerManager.GetOffloadServer("macos");
        if (targetName.Contains("Linux"))
            return BuildRunnerManager.GetOffloadServer("linux");

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
        private FileStream? _currentFileStream;
        private string? _currentFilePath;
        private int _currentTotalLength;
        private readonly TaskCompletionSource _completionSource = new();
        private readonly string _buildName;
        private readonly string _rootPath;

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
            _rootPath = rootDir.FullName;
        }

        private void CreateFileStream(string inDilePath)
        {
            var filePath = Path.Combine(_rootPath, inDilePath);
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
                fileInfo.Delete();

            if (fileInfo.Directory?.Exists is not true)
                fileInfo.Directory?.Create();

            _currentFileStream?.Dispose();
            _currentFileStream = new FileStream(filePath, FileMode.Create);
        }

        public void OnStringMessage(string targetName, long buildTime)
        {
            if (targetName == _buildName)
                TotalBuildTime = buildTime;
        }

        public void OnDataReceived(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);
            var targetName = reader.ReadString();
            var totalLength = reader.ReadInt32();
            var filePath = reader.ReadString();
            var fragmentLength = reader.ReadInt32();
            var fragment = reader.ReadBytes(fragmentLength);

            // ignore if not for target
            if (targetName != _buildName)
                return;

            if (_currentFilePath != filePath)
            {
                CreateFileStream(filePath);
                _currentFilePath = filePath;
            }

            // write file
            _currentFileStream?.Write(fragment, 0, fragment.Length);
            _currentTotalLength += fragment.Length;

            // log progress
            var percent = _currentTotalLength / (double)totalLength * 100;
            // Console.WriteLine(
            //     $"File download progress [{_name}]: {_currentFilePath} | {_currentTotalLength}/{totalLength} ({percent:0}%)"
            // );

            if (_currentTotalLength >= totalLength)
            {
                Console.WriteLine($"Download complete [{_buildName}]");
                if (!_completionSource.TrySetResult())
                {
                    Console.Write($"Result already set! [{_buildName}]");
                }
            }
        }
    }
}
