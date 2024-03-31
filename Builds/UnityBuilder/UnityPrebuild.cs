using System;
using System.Collections.Generic;
using System.IO;
using SharedLib;
using ProjectSettings = UnityBuilder.Settings.ProjectSettings;

namespace UnityBuilder;

public class UnityPrebuild
{
    public BuildVersions BuildVersions { get; set; } = new();

    public void RunCustom(string projectSettingsPath, bool standalone, bool android, bool ios)
    {
        var projectSettings = new ProjectSettings(projectSettingsPath);

        // set bundle version to same. It should be set by user
        BuildVersions.BundleVersion = projectSettings.GetBundleVersion();

        // standalone
        if (standalone)
            BuildVersions.Standalone = (projectSettings.GetStandaloneBuildNumber() + 1).ToString();

        // android
        if (android)
            BuildVersions.AndroidVersionCode = (
                projectSettings.GetAndroidBuildCode() + 1
            ).ToString();

        // iOS
        if (ios)
            BuildVersions.IPhone = (projectSettings.GetIphoneBuildNumber() + 1).ToString();

        Logger.Log($"New BundleVersion: {BuildVersions}");
    }

    // TODO: get this way to work
    public void RunWithUnityBatchMode(
        string projectPath,
        string unityVersion,
        bool standalone,
        bool android,
        bool ios
    )
    {
        var path = UnityPath.GetDefaultUnityPath(unityVersion);

        var customArgs = new List<string>();
        if (standalone)
            customArgs.Add("-standalone");
        if (android)
            customArgs.Add("-android");
        if (ios)
            customArgs.Add("-ios");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var log = Path.Combine(home, "ci-cache", "logs", "prebuild.log");

        var args = new UnityArgs
        {
            ExecuteMethod = "BuildSystem.PrebuildRunner.Run",
            ProjectPath = projectPath,
            LogPath = log,
            CustomArgs = customArgs.ToArray()
        };

        var unity = new UnityRunner(path);
        unity.Run(args);
    }
}
