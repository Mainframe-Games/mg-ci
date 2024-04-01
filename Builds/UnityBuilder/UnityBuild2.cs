using Deployment.Configs;
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
    private readonly int _subTarget;
    private readonly string[] _scenes;
    private readonly string[] _extraScriptingDefines;
    private readonly string _assetBundleManifestPath;
    private readonly int _buildOptions;

    private readonly string _buildOptionsPath;

    public string BuildPath { get; }

    public UnityBuild2(string projectPath, JToken target, string buildTargetName)
    {
        _projectPath = projectPath;
        _unityVersion = UnityVersion.Get(projectPath);
        _buildTargetFlag = GetBuildTargetFlag(buildTargetName);

        _name = target["Name"]?.ToString() ?? throw new NullReferenceException();
        _extension = target["Extension"]?.ToString() ?? throw new NullReferenceException();
        _productName = target["ProductName"]?.ToString() ?? throw new NullReferenceException();
        _target = target["Target"]?.Value<int>() ?? throw new NullReferenceException();
        _targetGroup = target["TargetGroup"]?.Value<int>() ?? throw new NullReferenceException();
        _subTarget = target["SubTarget"]?.Value<int>() ?? throw new NullReferenceException();
        _scenes = target["Scenes"]?.ToObject<string[]>() ?? throw new NullReferenceException();
        _extraScriptingDefines =
            target["ExtraScriptingDefines"]?.ToObject<string[]>()
            ?? throw new NullReferenceException();
        _assetBundleManifestPath =
            target["AssetBundleManifestPath"]?.ToString() ?? throw new NullReferenceException();
        _buildOptions =
            target["BuildOptions"]?.ToObject<int>() ?? throw new NullReferenceException();

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
            SubTarget = _subTarget == 0 ? "Player" : "Server",
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
            ["subtarget"] = target._subTarget,
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
