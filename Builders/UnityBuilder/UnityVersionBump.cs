using UnityBuilder.Settings;

namespace UnityBuilder;

public class UnityVersionBump(
    string projectSettingsPath,
    bool standalone,
    bool android,
    bool ios
    )
{
    public void Run(
        out string outBundle,
        out int outStandalone,
        out int outAndroid,
        out int outIos
    )
    {
        var projectSettings = new UnityProjectSettings(projectSettingsPath);

        // set bundle version to same. It should be set by user
        outBundle = projectSettings.GetBundleVersion() ?? throw new NullReferenceException();

        // standalone
        if (standalone)
            outStandalone = projectSettings.GetStandaloneBuildNumber() + 1;
        else
            outStandalone = projectSettings.GetStandaloneBuildNumber();

        // android
        if (android)
            outAndroid = projectSettings.GetAndroidBuildCode() + 1;
        else
            outAndroid = projectSettings.GetAndroidBuildCode();

        // iOS
        if (ios)
            outIos = projectSettings.GetIphoneBuildNumber() + 1;
        else
            outIos = projectSettings.GetIphoneBuildNumber();

        Console.WriteLine($"New BundleVersion: {outBundle}");
        Console.WriteLine($"New Standalone: {outStandalone}");
        Console.WriteLine($"New Android: {outAndroid}");
        Console.WriteLine($"New iOS: {outIos}");
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
