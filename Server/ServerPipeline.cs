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

        await RunPreBuildAsync();
        Logger.LogTitle("Pre Build Complete", [("time", $"{sw.ElapsedMilliseconds}ms")]);
        sw.Restart();

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

    private async Task RunBuildAsync()
    {
        var tasks = new List<Task>();

        foreach (var buildTarget in project.BuildTargets)
        {
            var task = new TaskCompletionSource<byte[]>();

            var runner = GetUnityRunner(buildTarget.Target);
            runner.SendJObject(
                new JObject
                {
                    ["TargetName"] = buildTarget.Name,
                    ["BuildTarget"] = buildTarget.Target.ToString(),
                    ["Project"] = JObject.FromObject(project),
                }
            );

            runner.OnDataMessage += data =>
            {
                task.SetResult(data);
            };

            tasks.Add(task.Task);
        }

        await Task.WhenAll(tasks);
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
}
