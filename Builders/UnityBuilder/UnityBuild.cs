using UnityBuilder.Settings;

namespace UnityBuilder;

public class UnityBuild
{
    private readonly string _projectPath;
    private readonly string _unityVersion;
    private readonly string _buildTargetFlag;

    private readonly string _name;
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
            CustomArgs = BuildPlayerOptions(),
        };

        // set product name
        var projectSettings = new UnityProjectSettings(_projectPath);
        projectSettings.SetProductName(_productName);
        projectSettings.SaveFile();

        var path = UnityPath.GetDefaultUnityPath(_unityVersion);
        var unity = new UnityRunner(path);
        unity.Run(args);

        if (unity.ExitCode == 0)
            return;

        Console.WriteLine($"Unity build failed. Code: {unity.ExitCode}: {unity.Message}");
        Environment.Exit(1);
    }

    private string[] BuildPlayerOptions()
    {
        var args = new List<string>();

        args.Add($"-productName {_productName}");

        if (_extraScriptingDefines is not null && _extraScriptingDefines.Length > 0)
        {
            args.Add("-extraScriptingDefines");
            foreach (var define in _extraScriptingDefines ?? [])
                args.Add($",\"{define}\"");
        }

        if (_scenes is not null && _scenes.Length > 0)
        {
            args.Add("-scenes");
            foreach (var scene in _scenes ?? [])
                args.Add($",\"{scene}\"");
        }

        if (!string.IsNullOrEmpty(_assetBundleManifestPath))
            args.Add($"-assetBundleManifestPath \"{_assetBundleManifestPath}\"");

        if (_buildOptions != 0)
            args.Add($"-options {_buildOptions}");

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
