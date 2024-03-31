namespace ServerClientShared;

public class UnityBuildTarget
{
    public string? Name { get; set; }

    // config
    public string? Extension { get; set; }
    public string? ProductName { get; set; }
    public Unity.BuildTarget Target { get; set; } = Unity.BuildTarget.StandaloneWindows64;
    public Unity.BuildTargetGroup TargetGroup { get; set; } = Unity.BuildTargetGroup.Standalone;
    public Unity.SubTarget SubTarget { get; set; } = Unity.SubTarget.Player;
    public List<string> Scenes { get; set; } = [];
    public List<string> ExtraScriptingDefines { get; set; } = [];
    public string? AssetBundleManifestPath { get; set; }
    public int BuildOptions { get; set; } = (int)Unity.BuildOptions.None;

    public Unity.BuildPlayerOptions BuildPlayerOptions(string buildPath)
    {
        return new Unity.BuildPlayerOptions
        {
            target = Target,
            subtarget = (int)SubTarget,
            locationPathName = Path.Combine(buildPath, $"{ProductName}{Extension}"),
            targetGroup = TargetGroup,
            assetBundleManifestPath = AssetBundleManifestPath,
            scenes = Scenes.ToArray(),
            extraScriptingDefines = ExtraScriptingDefines.ToArray(),
            options = (Unity.BuildOptions)BuildOptions
        };
    }
}
