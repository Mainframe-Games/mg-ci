using System.Diagnostics;
using AvaloniaAppMVVM.Data;
using Newtonsoft.Json.Linq;
using ServerClientShared;
using SharedLib;

namespace Server;

public class ServerPipeline(Project project, Workspace workspace)
{
    public async void Run()
    {
        var sw = Stopwatch.StartNew();

        // version bump
        var fullVersion = await RunPreBuildAsync();
        Logger.LogTitle(
            "Pre Build Complete",
            [("time", $"{sw.ElapsedMilliseconds.ToHourMinSecString()}")]
        );
        sw.Restart();

        // changelog
        var changeLog = BuildChangeLog();

        // builds
        var processes = await RunBuildAsync();
        Logger.LogTitle(
            "Build Complete",
            [("time", $"{sw.ElapsedMilliseconds.ToHourMinSecString()}")]
        );
        sw.Restart();

        // deploys
        RunDeploy(processes, fullVersion, changeLog);
    }

    private string[] BuildChangeLog()
    {
        switch (workspace)
        {
            case PlasticWorkspace plastic:
                plastic.GetCurrent(out var cs, out var guid);
                var prevCs = workspace.Meta.LastSuccessfulBuild.GetValueOrDefault();
                return plastic.GetChangeLog(cs, prevCs);
            case GitWorkspace git:
                return git.GetChangeLog();
        }

        throw new Exception("Unknown workspace type!");
    }

    #region Prebuild

    private async Task<string> RunPreBuildAsync()
    {
        // TODO: this could probably be done on main server
        var task = new TaskCompletionSource<JObject>();
        BuildRunnerFactory.VersionBump.SendJObject(JObject.FromObject(project));
        BuildRunnerFactory.VersionBump.OnStringMessage += message =>
        {
            task.SetResult(JObject.Parse(message));
        };

        var res = await task.Task;
        
        var bundle = res["Bundle"]?.ToString();
        var standalone = res["Standalone"]?.ToString();
        var android = res["Android"]?.ToString();
        var ios = res["Ios"]?.ToString();

        // save files
        workspace.ProjectSettings.ReplaceVersions(
            bundle,
            standalone,
            android,
            ios
        );

        var fullVersion = $"{bundle}.{standalone}";
        workspace.SaveBuildVersion(fullVersion);

        // commit file
        workspace.GetCurrent(out var csId, out var guid);
        switch (workspace)
        {
            case PlasticWorkspace:
                workspace.Commit($"_Build Version: {fullVersion} | cs: {csId} | guid: {guid}");
                break;
            case GitWorkspace gitWorkspace:
                gitWorkspace.Commit(
                    $"_Build Version: {fullVersion} | sha: {guid}",
                    new[] { workspace.ProjectSettingsPath, gitWorkspace.BuildVersionPath }
                );
                break;
        }

        return fullVersion;
    }

    #endregion

    #region Build

    private async Task<List<BuildRunnerProcess>> RunBuildAsync()
    {
        var buildProcesses = new List<BuildRunnerProcess>();
        var tasks = new List<Task>();

        // assign callbacks
        foreach (var runner in BuildRunnerFactory.All)
        {
            runner.OnStringMessage += message =>
            {
                foreach (var process in buildProcesses)
                    process.OnStringMessage(message);
            };
            runner.OnDataMessage += bytes =>
            {
                foreach (var process in buildProcesses)
                    process.OnDataReceived(bytes);
            };
        }

        // start processes
        foreach (var buildTarget in project.BuildTargets)
        {
            var runner = GetUnityRunner(buildTarget.Target);
            runner.SendJObject(
                new JObject
                {
                    ["TargetName"] = buildTarget.Name,
                    ["BuildTarget"] = buildTarget.Target.ToString(),
                    ["Project"] = JObject.FromObject(project),
                }
            );

            var process = new BuildRunnerProcess(buildTarget.Name!);
            buildProcesses.Add(process);
            tasks.Add(process.Task);
        }

        // wait for them all to finish
        await Task.WhenAll(tasks);

        // clear callbacks
        foreach (var runner in BuildRunnerFactory.All)
            runner.ClearEvents();

        return buildProcesses;
    }

    private static WebClient GetUnityRunner(Unity.BuildTarget target)
    {
        switch (target)
        {
            case Unity.BuildTarget.StandaloneWindows64:
            case Unity.BuildTarget.StandaloneWindows:
                return BuildRunnerFactory.GetRunner("windows");

            case Unity.BuildTarget.iOS:
            case Unity.BuildTarget.StandaloneOSX:
                return BuildRunnerFactory.GetRunner("macos");

            case Unity.BuildTarget.StandaloneLinux64:
                return BuildRunnerFactory.GetRunner("linux");

            default:
                throw new NotSupportedException($"Target not supported: {target}");
        }
    }

    #endregion

    #region Deploy

    private void RunDeploy(
        List<BuildRunnerProcess> buildProcesses,
        string fullVersion,
        string[] changeLog
    )
    {
        var deployRunner = new DeploymentRunner(project, workspace, fullVersion, changeLog);
        deployRunner.Deploy();
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

            var rootPath = Path.Combine(App.CiCachePath, "Deploy", _buildName);
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

        public void OnStringMessage(string message)
        {
            var jObj = JObject.Parse(message);

            var targetName = jObj["TargetName"]?.ToString();
            if (targetName != _buildName)
                return;

            var status = jObj["Status"]?.ToString();

            if (status == "Complete")
                TotalBuildTime = jObj["Time"]?.Value<long>() ?? 0;
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
