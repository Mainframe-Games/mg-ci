using Utils.Unity;

namespace MainServer.VersionBumping;

internal class UnityVersionBump(string projectPath, bool standalone, bool android, bool ios)
{
    public string ProjectSettingsPath { get; } =
        Path.Combine(projectPath, "ProjectSettings", "ProjectSettings.asset");

    /// <summary>
    /// Returns full version {bundle}.{standalone}
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public string Run()
    {
        var projectSettings = new UnityProjectSettings(projectPath);

        // set bundle version to same. It should be set by user
        var outBundle = projectSettings.GetBundleVersion() ?? throw new NullReferenceException();
        Console.WriteLine($"New BundleVersion: {outBundle}");
        projectSettings.WriteBundleVersion(outBundle);

        int outStandalone = 0;

        // standalone
        if (standalone)
        {
            outStandalone = projectSettings.GetStandaloneBuildNumber() + 1;
            Console.WriteLine($"New Standalone: {outStandalone}");
            projectSettings.WritePlatformBuildNumber("Standalone", outStandalone);
        }

        // android
        if (android)
        {
            var outAndroid = projectSettings.GetAndroidBuildCode() + 1;
            Console.WriteLine($"New Android: {outAndroid}");
            projectSettings.WriteAndroidBundleVersionCode(outAndroid);
        }

        // iOS
        if (ios)
        {
            var outIos = projectSettings.GetIphoneBuildNumber() + 1;
            Console.WriteLine($"New iOS: {outIos}");
            projectSettings.WritePlatformBuildNumber("iPhone", outIos);
        }

        projectSettings.SaveFile();
        return $"{outBundle}.{outStandalone}";
    }
}
