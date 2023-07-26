using SharedLib;

namespace Deployment.Configs;

/// <summary>
/// Build config local to each Unity project
/// </summary>
public sealed class BuildConfig : Yaml
{
	public PreBuildConfig? PreBuild { get; }
	public ParallelBuildConfig? ParallelBuild { get; }
	public DeployContainerConfig? Deploy { get; }
	public HooksConfig[]? Hooks { get; }
	
	public BuildConfig(string? path, int skip = 3) : base(path, skip)
	{
		PreBuild = GetObject<PreBuildConfig>(nameof(PreBuild));
		// PreBuild = new PreBuildConfig
		// {
		// 	BumpIndex = GetValue<int>($"{nameof(PreBuild)}.{nameof(PreBuild.BumpIndex)}")
		// };
		// ParallelBuild = GetObject<ParallelBuildConfig>(nameof(ParallelBuild));
		Deploy = GetObject<DeployContainerConfig>(nameof(Deploy));
		Hooks = GetObject<HooksConfig[]>(nameof(Hooks));
	}
	
	public override T GetValue<T>(string path)
	{
		return base.GetValue<T>($"MonoBehaviour.{path}");
	}

	public override T? GetObject<T>(string path) where T : class
	{
		return base.GetObject<T>($"MonoBehaviour.{path}");
	}

	public static BuildConfig GetConfig(string workingDirectory)
	{
		var path = Path.Combine(workingDirectory, "Assets", "Settings", "BuildSettings", "BuildConfig.asset");
		return new BuildConfig(path);
	}
}

public class PreBuildConfig
{
	public int BumpIndex { get; set; }
	public VersionsConfig Versions { get; set; }
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

public class VersionsConfig
{
	public bool? BundleVersion { get; set; }
	public bool? AndroidVersionCode { get; set; }
	public string[]? BuildNumbers { get; set; }
}
