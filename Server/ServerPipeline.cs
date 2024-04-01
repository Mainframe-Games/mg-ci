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
        PrepareWorkspace(workspace, project);

        var sw = Stopwatch.StartNew();

        // await RunPreBuildAsync();
        // Logger.LogTitle("Pre Build Complete", [("time", $"{sw.ElapsedMilliseconds}ms")]);
        // sw.Restart();

        await RunBuildAsync();
        Logger.LogTitle("Build Complete", [("time", $"{sw.ElapsedMilliseconds}ms")]);
        // RunDeploy();
    }

    private static void PrepareWorkspace(Workspace workspace, Project project)
    {
        workspace.Clear();
        workspace.SwitchBranch(project.Settings.Branch!);
        workspace.Update();
    }

    #region Prebuild

    private async Task RunPreBuildAsync()
    {
        var task = new TaskCompletionSource<JObject>();
        BuildRunnerFactory.VersionBump.SendJObject(JObject.FromObject(project));
        BuildRunnerFactory.VersionBump.OnStringMessage += message =>
        {
            task.SetResult(JObject.Parse(message));
        };

        var res = await task.Task;
        /*
         {
          "bundle": "0.1",
          "standalone": 19,
          "android": 1,
          "ios": 0
         }
         */

        // save files
        workspace.ProjectSettings.ReplaceVersions(
            res["bundle"]?.ToString(),
            res["standalone"]?.ToString(),
            res["android"]?.ToString(),
            res["ios"]?.ToString()
        );

        var fullVersion = $"{res["bundle"]}.{res["standalone"]}";
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
    }

    #endregion

    #region Build

    private readonly List<BuildRunnerProcess> _buildProcesses = [];

    private void OnBuildMessageReceived(string message)
    {
        foreach (var process in _buildProcesses)
            process.OnMessageReceived(message);
    }

    private void OnBuildDataReceived(byte[] bytes)
    {
        foreach (var process in _buildProcesses)
            process.OnDataReceived(bytes);
    }

    private async Task RunBuildAsync()
    {
        var tasks = new List<Task>();

        // assign callbacks
        foreach (var runner in BuildRunnerFactory.All)
        {
            runner.OnStringMessage += OnBuildMessageReceived;
            runner.OnDataMessage += OnBuildDataReceived;
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

            var process = new BuildRunnerProcess( buildTarget.Name);
            _buildProcesses.Add(process);
            tasks.Add(process.Task);
        }

        // wait for them all to finish
        await Task.WhenAll(tasks);

        // assign callbacks
        foreach (var runner in BuildRunnerFactory.All)
            runner.ClearEvents();
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

    private void RunDeploy() { }

    #endregion
    
    private class BuildRunnerProcess
    {
        private FileStream? _currentFileStream;
        private string? _currentFilePath;
        private int _currentTotalLength;
        private readonly TaskCompletionSource _completionSource = new();
        private readonly string _name;
        private readonly string _rootPath;

        public Task Task => _completionSource.Task;

        public BuildRunnerProcess(string name)
        {
            _name = name;
            
            var rootPath = Path.Combine(App.CiCachePath, "Deploy", _name);
            var rootDir = new DirectoryInfo(rootPath);
            if (rootDir.Exists)
                rootDir.Delete(true);
            _rootPath = rootDir.FullName;
        }
        
        public void OnMessageReceived(string message)
        {
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
            if (targetName != _name)
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
                Console.WriteLine($"Download complete [{_name}]");
                if (!_completionSource.TrySetResult())
                {
                    Console.Write($"Result already set! [{_name}]");
                }
            }
        }
    }
}
