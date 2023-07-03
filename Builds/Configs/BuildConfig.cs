using SharedLib;

namespace Deployment.Configs;

/// <summary>
/// Build config local to each Unity project
/// </summary>
public class BuildConfig
{
	public PreBuildConfig? PreBuild { get; set; }
	public PostBuildConfig? PostBuild { get; set; }
	public ParallelBuildConfig? ParallelBuild { get; set; }
	public TargetConfig[]? Builds { get; set; }
	public DeployContainerConfig? Deploy { get; set; }
	public HooksConfig[]? Hooks { get; set; }

	public static BuildConfig GetConfig(string? workingDirectory)
	{
		if (workingDirectory == null)
			return new BuildConfig();
		
		var path = Path.Combine(workingDirectory, "BuildScripts", "buildconfig.json");
		var configStr = File.ReadAllText(path);
		var configClass = Json.Deserialise<BuildConfig>(configStr);

		if (configClass == null)
			throw new NullReferenceException("Failed to parse buildconfig.json");
		
		return configClass;
	}

	public TargetConfig GetBuildTarget(UnityTarget target, bool isServer = false)
	{
		foreach (var build in Builds)
		{
			if (build.Target != target)
				continue;
			
			if (isServer && !build.Settings.Contains("Server", StringComparison.OrdinalIgnoreCase))
				continue;
				
			return build;
		}

		throw new Exception($"Target not found: {target}");
	}
}

public class PreBuildConfig
{
	public int BumpIndex { get; set; }
	public VersionsConfig Versions { get; set; }
}

public class PostBuildConfig
{
	public bool ChangeLog { get; set; }
}

public class ParallelBuildConfig
{
	/// <summary>
	/// Additional directories to symlink
	/// </summary>
	public string[]? Links { get; set; }

	/// <summary>
	/// Additional directories to copy
	/// </summary>
	public string[]? Copies { get; set; }
}

public class DeployContainerConfig
{
	public string[]? Steam { get; set; }
	public bool? Clanforge { get; set; }
	public bool? AppleStore { get; set; }
	public bool? GoogleStore { get; set; }
	public bool? S3 { get; set; }
}

public class HooksConfig
{
	public string? Url { get; set; }
	public string? Title { get; set; }
	public bool IsErrorChannel { get; set; }

	public bool IsDiscord() => Url?.StartsWith("https://discord.com/") ?? false;
	public bool IsSlack() => Url?.StartsWith("https://hooks.slack.com/") ?? false;
}

public class TargetConfig
{
	public UnityTarget? Target { get; set; }
	public string? Settings { get; set; }
	public string? ExecuteMethod { get; set; }
	[Obsolete("Move towards using .asset YAML from disk")]
	public string? BuildPath { get; set; }
	public string? VersionExtension { get; set; }

	public BuildSettingsAsset GetBuildSettingsAsset(string? buildSettingsDir)
	{
		var path = Path.Combine(buildSettingsDir, $"{Settings}.asset");
		var asset = new BuildSettingsAsset(path);
		return asset;
	}
}

public class VersionsConfig
{
	public bool? BundleVersion { get; set; }
	public bool? AndroidVersionCode { get; set; }
	public string[]? BuildNumbers { get; set; }
}

/// <summary>
/// Src: https://docs.unity3d.com/Manual/EditorCommandLineArguments.html Build Arguments
/// </summary>
public enum UnityTarget
{
	None,
	Standalone,
	Win,
	Win64,
	OSXUniversal,
	Linux64,
	iOS,
	Android,
	WebGL,
	WindowsStoreApps,
	tvOS
}