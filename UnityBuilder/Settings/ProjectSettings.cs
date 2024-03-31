using SharedLib;

namespace UnityBuilder.Settings;

public class ProjectSettings : Yaml
{
    public ProjectSettings(string? path)
        : base(path) { }

    /// <summary>
    /// Replaces the version in all the places within ProjectSettings.asset
    /// </summary>
    public void ReplaceVersions(BuildVersions? buildVersions)
    {
        if (buildVersions == null)
            return;

        WriteBundleVersion(buildVersions.BundleVersion);

        // standalone
        if (!string.IsNullOrEmpty(buildVersions.Standalone))
            WritePlatformBuildNumber("Standalone", buildVersions.Standalone);

        // ios
        if (!string.IsNullOrEmpty(buildVersions.IPhone))
            WritePlatformBuildNumber("iPhone", buildVersions.IPhone);

        // android
        if (!string.IsNullOrEmpty(buildVersions.AndroidVersionCode))
            WriteAndroidBundleVersionCode(buildVersions.AndroidVersionCode);

        SaveToFile();
    }

    public override T GetValue<T>(string path)
    {
        return base.GetValue<T>($"PlayerSettings.{path}");
    }

    public int GetStandaloneBuildNumber()
    {
        var v = GetValue<string>("buildNumber.Standalone");
        return int.TryParse(v, out var num) ? num : 0;
    }

    public int GetAndroidBuildCode()
    {
        return GetValue<int>("AndroidBundleVersionCode");
    }

    public int GetIphoneBuildNumber()
    {
        var v = GetValue<string>("buildNumber.iPhone");
        return int.TryParse(v, out var num) ? num : 0;
    }

    public string? GetBundleVersion()
    {
        return GetValue<string?>("bundleVersion");
    }

    private void WriteBundleVersion(string? newBundleVersion)
    {
        var index = GetProjPropertyLineIndex("bundleVersion");
        _lines[index] = ReplaceText(_lines[index], newBundleVersion);
    }

    private void WritePlatformBuildNumber(string platform, string? newBundleVersion)
    {
        var index = GetProjPropertyLineIndex("buildNumber", platform);
        _lines[index] = ReplaceText(_lines[index], newBundleVersion);
    }

    private void WriteAndroidBundleVersionCode(string? androidVersionCode)
    {
        var index = GetProjPropertyLineIndex("AndroidBundleVersionCode");
        _lines[index] = ReplaceText(_lines[index], androidVersionCode);
    }
}
