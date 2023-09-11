using Deployment.Configs;
using SharedLib;

namespace Builds;

public class PreBuild
{
	private readonly Workspace _workspace;
	public BuildVersions BuildVersions { get; } = new();

	public PreBuild(Workspace workspace)
	{
		_workspace = workspace;
	}

	public void Run(PreBuildConfig? config)
	{
		if (config == null) throw new NullReferenceException("Config con not be null");

		BuildVersions.BundleVersion = _workspace.GetBundleVersion();

		// standalone
		if (config.BuildNumberStandalone is true)
			BuildVersions.Standalone = (_workspace.GetStandaloneBuildNumber() + 1).ToString();

		// android
		if (config.AndroidVersionCode is true)
			BuildVersions.AndroidVersionCode = (_workspace.GetAndroidBuildCode() + 1).ToString();

		// iOS
		if (config.BuildNumberIphone is true)
			BuildVersions.IPhone = (_workspace.GetIphoneBuildNumber() + 1).ToString();

		Logger.Log($"New BundleVersion: {BuildVersions}");
	}
}