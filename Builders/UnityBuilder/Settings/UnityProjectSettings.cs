using SharedLib;

namespace UnityBuilder.Settings;

internal class UnityProjectSettings(string? path) : Yaml(path)
{
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
}
