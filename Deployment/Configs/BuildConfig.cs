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
	public SteamConfig? Steam { get; set; }
	public string? OffloadUrl { get; set; }
	public TargetConfig[]? Targets { get; set; }
}