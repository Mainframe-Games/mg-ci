using Deployment.PreBuild;

namespace Deployment.Configs;

/// <summary>
/// Build config local to each Unity project
/// </summary>
public class BuildConfig
{
	public PreBuildType PreBuildType { get; set; }
	public TargetConfig[]? Builds { get; set; }
	public DeployContiner? Deploy { get; set; }
	public HooksConfig[]? Hooks { get; set; }
}


public class DeployContiner
{
	public SteamConfig? Steam { get; set; }
	public MultiplayConfigLocal? Multiplay { get; set; }
}
