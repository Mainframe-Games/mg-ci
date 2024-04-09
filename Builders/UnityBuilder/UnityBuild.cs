using UnityBuilder.Settings;

namespace UnityBuilder;

public class UnityBuild
{
    private readonly string _projectPath;
    private readonly string _unityVersion;
    private readonly string _buildTargetFlag;

    private readonly string _name;
    private readonly string _extension;
    private readonly string _productName;
    private readonly int _target;
    private readonly int _targetGroup;
    private readonly string _subTarget;
    private readonly string[]? _scenes;
    private readonly string[]? _extraScriptingDefines;
    private readonly string? _assetBundleManifestPath;
    private readonly int _buildOptions;

    public string BuildPath { get; }

    public UnityBuild(
        string projectPath,
        string name,
        // player build options
        string extension,
        string productName,
        string buildTargetName,
        string targetGroup,
        string subTarget,
        string[] scenes,
        string[] extraScriptingDefines,
        string assetBundleManifestPath,
        int buildOptions
    )
    {
        _projectPath = projectPath;
        _unityVersion = UnityVersion.Get(projectPath);
        _buildTargetFlag = GetBuildTargetFlag(buildTargetName);

        _name = name;
        _extension = extension;
        _productName = productName;
        // _target = GetTargetEnumValue(buildTargetName);
        // _targetGroup = GetTargetGroupEnumValue(targetGroup);
        _subTarget = subTarget;
        _scenes = scenes;
        _extraScriptingDefines = extraScriptingDefines;
        _assetBundleManifestPath = assetBundleManifestPath;
        _buildOptions = buildOptions;

        // plant current build target settings in project
        BuildPath = Path.Combine(projectPath, "Builds", _name);
    }

    public void Run()
    {
        var logPath = Path.Combine(_projectPath, "Builds", "Logs", $"build_{_name}.log");

        var args = new UnityArgs
        {
            ExecuteMethod = "Mainframe.CI.Editor.BuildScript.BuildPlayer",
            ProjectPath = _projectPath,
            LogPath = logPath,
            BuildPath = BuildPath,
            BuildTarget = _buildTargetFlag,
            SubTarget = _subTarget,
            CustomArgs = BuildPlayerOptions(BuildPath, this),
        };

        var path = UnityPath.GetDefaultUnityPath(_unityVersion);
        var unity = new UnityRunner(path);
        unity.Run(args);

        if (unity.ExitCode == 0)
            return;

        Console.WriteLine($"Unity build failed. Code: {unity.ExitCode}: {unity.Message}");
        Environment.Exit(1);
    }

    private static string[] BuildPlayerOptions(string buildPath, UnityBuild target)
    {
        var args = new List<string>();

        var locationPathName = Path.Combine(buildPath, $"{target._productName}{target._extension}");
        args.Add($"-locationPathName \"{locationPathName}\"");

        if (target._extraScriptingDefines is not null && target._extraScriptingDefines.Length > 0)
        {
            args.Add("-extraScriptingDefines");
            foreach (var define in target._extraScriptingDefines ?? [])
                args.Add($",\"{define}\"");
        }

        if (target._scenes is not null && target._scenes.Length > 0)
        {
            args.Add("-scenes");
            foreach (var scene in target._scenes ?? [])
                args.Add($",\"{scene}\"");
        }

        if (!string.IsNullOrEmpty(target._assetBundleManifestPath))
            args.Add($"-assetBundleManifestPath \"{target._assetBundleManifestPath}\"");

        if (target._buildOptions != 0)
            args.Add($"-options {target._buildOptions}");

        return args.ToArray();
    }

    /// <summary>
    /// Src: https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static string GetBuildTargetFlag(string buildTargetName)
    {
        if (buildTargetName.Contains("OSX"))
            return "OSXUniversal";
        if (buildTargetName.Contains("Windows"))
            return "Win64";
        if (buildTargetName.Contains("Linux"))
            return "Linux64";
        if (buildTargetName.Contains("iOS"))
            return "iOS";
        if (buildTargetName.Contains("Android"))
            return "Android";

        throw new NotSupportedException($"Target not supported: {buildTargetName}");
    }
}
