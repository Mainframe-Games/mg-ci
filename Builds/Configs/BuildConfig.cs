using SharedLib;

namespace Deployment.Configs;

/// <summary>
/// Build config local to each Unity project
/// </summary>
public class BuildConfig
{
	public PreBuild? PreBuild { get; set; }
	public PostBuild? PostBuild { get; set; }
	public string[]? Links { get; set; }
	public TargetConfig[]? Builds { get; set; }
	public DeployContiner? Deploy { get; set; }
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
}

public class PreBuild
{
	public PreBuildType Type { get; set; }
}

public class PostBuild
{
	public bool ChangeLog { get; set; }
}

public class DeployContiner
{
	public string[]? Steam { get; set; }
	public bool? Clanforge { get; set; }
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
	public string? BuildPath { get; set; }
	public string? VersionExtension { get; set; }
}

public enum UnityTarget
{
	None,
	Win64,
	OSXUniversal,
	Linux64
}

public enum PreBuildType
{
	/// <summary>
	/// No prebuild
	/// </summary>
	None,
	
	/// <summary>
	/// MAJOR - Single number increments
	/// </summary>
	Major,
	
	/// <summary>
	/// MAJOR.MINOR - Minor version increments. MAJOR must be done manually.
	/// </summary>
	Major_Minor,
	
	// TODO: add MAJOR_MINOR_PATCH
}