using AvaloniaAppMVVM.Data;
using Newtonsoft.Json.Linq;
using ServerClientShared;
using SharedLib;
using UnityBuilder;

namespace Server;

public class ServerPipeline(Project project, Workspace workspace)
{
    public void Run()
    {
        PrepareWorkspace(workspace, project);
        RunPreBuild();
        Logger.LogTitle("Pre Build Complete");
        RunBuild();
        Logger.LogTitle("Build Complete");
    }

    private static void PrepareWorkspace(Workspace workspace, Project project)
    {
        workspace.Clear();
        workspace.Update();
        workspace.SwitchBranch(project.Settings.Branch!);
    }

    #region Prebuild

    private void RunPreBuild()
    {
        // TODO: this should eventually be done on the build runner server
        switch (project.Settings.GameEngine)
        {
            case GameEngineType.Unity:
                UnityPrebuild();
                break;
            case GameEngineType.Godot:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UnityPrebuild()
    {
        // run pre-build
        var preBuildProcess = new UnityPrebuild();
        preBuildProcess.RunCustom(
            workspace.ProjectSettingsPath,
            project.Prebuild.BuildNumberStandalone,
            project.Prebuild.AndroidVersionCode,
            project.Prebuild.BuildNumberIphone
        );

        var versions = preBuildProcess.BuildVersions;
        var fullVersion = versions.FullVersion;

        // save files
        workspace.ProjectSettings.ReplaceVersions(versions);
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

    private void RunBuild()
    {
        switch (project.Settings.GameEngine)
        {
            case GameEngineType.Unity:
                UnityBuild();
                break;
            case GameEngineType.Godot:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UnityBuild()
    {
        foreach (var buildTarget in project.BuildTargets)
        {
            // plant current build target settings in project
            var buildPath = Path.Combine(workspace.Directory, "Builds", buildTarget.Name);
            var settingsJson = JObject
                .FromObject(buildTarget.BuildPlayerOptions(buildPath))
                .ToString();
            var settingsPath = Path.Combine(workspace.Directory, ".ci", "build_options.json");
            File.WriteAllText(settingsPath, settingsJson);

            // run build
            var unityRunner = new UnityBuild2();
            var targetFlag = GetBuildTargetFlag(buildTarget.Target);
            unityRunner.Run(
                workspace.Directory,
                workspace.UnityVersion,
                targetFlag,
                buildTarget.SubTarget.ToString(),
                buildPath
            );

            // clear build settings
            File.Delete(settingsPath);
        }
    }

    /// <summary>
    /// Src: https://docs.unity3d.com/Manual/EditorCommandLineArguments.html Build Arguments
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static string GetBuildTargetFlag(Unity.BuildTarget target)
    {
        switch (target)
        {
            case Unity.BuildTarget.StandaloneOSX:
            case Unity.BuildTarget.StandaloneOSXIntel:
            case Unity.BuildTarget.StandaloneOSXIntel64:
                return "OSXUniversal";

            case Unity.BuildTarget.StandaloneWindows:
            case Unity.BuildTarget.StandaloneWindows64:
                return "Win64";

            case Unity.BuildTarget.EmbeddedLinux:
            case Unity.BuildTarget.StandaloneLinux:
            case Unity.BuildTarget.StandaloneLinux64:
            case Unity.BuildTarget.StandaloneLinuxUniversal:
                return "Linux64";

            case Unity.BuildTarget.iOS:
                return "iOS";

            case Unity.BuildTarget.Android:
                return "Android";
        }

        throw new NotSupportedException($"Target not supported: {target}");
    }

    #endregion
}
