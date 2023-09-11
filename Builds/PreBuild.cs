using Deployment.Configs;
using SharedLib;

namespace Builds;

public class PreBuild
{
	private readonly Workspace _workspace;
	private readonly Args _args;
	public BuildVersions BuildVersions { get; } = new();

	public PreBuild(Workspace workspace, Args args)
	{
		_workspace = workspace;
		_args = args;
	}

	public void Run(PreBuildConfig? config)
	{
		if (config == null)
            throw new NullReferenceException("Config con not be null");

		// bundle version
		if (_args.TryGetArg("-bundleversion", out var i) && int.TryParse(i, out var index))
		{
			/*
			 * For bundle version changes we need to reset all build numbers back to 0 as its a new bundle
			 */
			
			var versionArray = _workspace.GetVersionArray();
			versionArray[index]++;
			BuildVersions.BundleVersion = string.Join(".", versionArray);
			
			// standalone
			if (config.BuildNumberStandalone is true)
				BuildVersions.Standalone = "0";

			// android
			if (config.AndroidVersionCode is true)
				BuildVersions.AndroidVersionCode = "0";

			// iOS
			if (config.BuildNumberIphone is true)
				BuildVersions.IPhone = "0";
		}
		else
		{
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
		}

		Logger.Log($"New BundleVersion: {BuildVersions}");
	}
}