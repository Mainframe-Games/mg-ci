using Deployment.PreBuild;

namespace Deployment.Configs;

public class BuildConfig
{
	public PreBuildType PreBuildType { get; set; }
	public BuildContainer[] Builds { get; set; }
	public HooksConfig? Hooks { get; set; }

	public void Validate()
	{
		foreach (var build in Builds)
			build.Validate();
	}
}

public class BuildContainer
{
	public SteamConfig Steam { get; set; }
	public string UnityPath { get; set; }
	private string ExecuteMethod { get; set; }
	public TargetConfig[] Targets { get; set; }

	public void Validate()
	{
		// if no Unity path is set, use default one
		if (string.IsNullOrEmpty(ExecuteMethod))
			ExecuteMethod = "BuildSystem.BuildScript.BuildPlayer";
		
		// if no Unity path is set, use default one
		if (string.IsNullOrEmpty(UnityPath))
			UnityPath = GetDefaultUnityPath();
		
		// assign default properties if no overrides are set
		foreach (var targetConfig in Targets)
		{
			if (!string.IsNullOrEmpty(UnityPath) && string.IsNullOrEmpty(targetConfig.UnityPath))
				targetConfig.UnityPath = UnityPath;
			if (!string.IsNullOrEmpty(ExecuteMethod) && string.IsNullOrEmpty(targetConfig.ExecuteMethod))
				targetConfig.ExecuteMethod = ExecuteMethod;
		}
	}

	private static string GetDefaultUnityPath()
	{
		return OperatingSystem.IsMacOS()
			? "/Applications/Unity/Hub/Editor/{unityVersion}/Unity.app/Contents/MacOS/Unity"
			: @"C:\Program Files\Unity\Hub\Editor\{unityVersion}\Editor\Unity.exe";
	}
}