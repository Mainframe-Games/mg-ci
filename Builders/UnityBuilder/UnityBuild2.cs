using Newtonsoft.Json.Linq;
using UnityBuilder.Settings;

namespace UnityBuilder;

public class UnityBuild2
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
    private readonly string[] _scenes;
    private readonly string[] _extraScriptingDefines;
    private readonly string _assetBundleManifestPath;
    private readonly int _buildOptions;

    private readonly string _buildOptionsPath;

    public string BuildPath { get; }

    public UnityBuild2(
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
        _target = GetTargetEnumValue(buildTargetName);
        _targetGroup = GetTargetGroupEnumValue(targetGroup);
        _subTarget = subTarget;
        _scenes = scenes;
        _extraScriptingDefines = extraScriptingDefines;
        _assetBundleManifestPath = assetBundleManifestPath;
        _buildOptions = buildOptions;

        // plant current build target settings in project
        BuildPath = Path.Combine(projectPath, "Builds", _name);
        var settingsJson = BuildPlayerOptions(BuildPath, this);
        _buildOptionsPath = Path.Combine(projectPath, ".ci", "build_options.json");
        File.WriteAllText(_buildOptionsPath, settingsJson.ToString());
    }

    public void Run()
    {
        var logPath = Path.Combine(_projectPath, "Builds", "Logs", $"build_{_name}.log");

        var args = new UnityArgs
        {
            ExecuteMethod = "BuildSystem.BuildScript.BuildPlayer",
            ProjectPath = _projectPath,
            LogPath = logPath,
            BuildPath = BuildPath,
            BuildTarget = _buildTargetFlag,
            SubTarget = _subTarget,
            CustomArgs = null
        };

        var path = UnityPath.GetDefaultUnityPath(_unityVersion);
        var unity = new UnityRunner(path);
        unity.Run(args);

        // clear build settings
        File.Delete(_buildOptionsPath);

        if (unity.ExitCode == 0)
            return;

        Console.WriteLine($"Unity build failed. Code: {unity.ExitCode}: {unity.Message}");
        Environment.Exit(1);
    }

    private static JObject BuildPlayerOptions(string buildPath, UnityBuild2 target)
    {
        return new JObject
        {
            ["target"] = target._target,
            ["subtarget"] = GetSubTargetEnumValue(target._subTarget),
            ["locationPathName"] = Path.Combine(
                buildPath,
                $"{target._productName}{target._extension}"
            ),
            ["targetGroup"] = target._targetGroup,
            ["assetBundleManifestPath"] = target._assetBundleManifestPath,
            ["scenes"] = JArray.FromObject(target._scenes),
            ["extraScriptingDefines"] = JArray.FromObject(target._extraScriptingDefines),
            ["options"] = target._buildOptions
        };
    }

    private static int GetTargetEnumValue(string buildTargetName)
    {
        return 0;
    }

    /// <summary>
    /// docs: https://docs.unity3d.com/ScriptReference/BuildTarget.html
    /// </summary>
    /// <param name="subTarget"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static int GetSubTargetEnumValue(string subTarget)
    {
        return subTarget switch
        {
            "StandaloneWindows" => 5,
            "StandaloneWindows64" => 14,

            "StandaloneLinux64" => 24,

            "StandaloneOSX" => 2,

            "iOS" => 9,
            "Android" => 13,

            "WebGL" => 20,

            _ => throw new NotSupportedException($"Target Group not supported: {subTarget}")
        };
    }

    private static int GetTargetGroupEnumValue(string targetGroup)
    {
        return targetGroup switch
        {
            "Standalone" => 1,
            "iOS" => 4,
            "Android" => 7,
            _ => throw new NotSupportedException($"Target Group not supported: {targetGroup}")
        };
    }

    /// <summary>
    /// Src: https://docs.unity3d.com/Manual/EditorCommandLineArguments.html Build Arguments
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
