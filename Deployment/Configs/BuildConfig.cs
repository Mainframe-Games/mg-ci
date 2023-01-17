using Deployment.PreBuild;

namespace Deployment.Configs;

public class BuildConfig
{
	public PreBuildType PreBuildType { get; set; }
	public BuildContainer[]? Builds { get; set; }
	public HooksConfig[]? Hooks { get; set; }
}

public class BuildContainer
{
	public SteamConfig Steam { get; set; }
	public string OffloadUrl { get; set; }
	public TargetConfig[] Targets { get; set; }

	public static string GetDefaultExecuteMethod()
	{
		return "BuildSystem.BuildScript.BuildPlayer";
	}

	public static string GetDefaultUnityPath()
	{
		return OperatingSystem.IsMacOS()
			? "/Applications/Unity/Hub/Editor/{unityVersion}/Unity.app/Contents/MacOS/Unity"
			: @"C:\Program Files\Unity\Hub\Editor\{unityVersion}\Editor\Unity.exe";
	}
}