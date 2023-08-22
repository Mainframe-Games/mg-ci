using Deployment.Configs;
using SharedLib;

namespace Builds;

public class PreBuild
{
	private readonly Workspace _workspace;
	public BuildVersions BuildVersion { get; }

	public PreBuild(Workspace workspace)
	{
		_workspace = workspace;
		BuildVersion = new();
	}

	public void Run(PreBuildConfig? config)
	{
		if (config == null)
			throw new NullReferenceException("Config con not be null");
		if (config.Versions == null)
			throw new NullReferenceException("Config.Versions con not be null");

		var verArr = _workspace.GetVersionArray();
		verArr[config.BumpIndex]++;
		BuildVersion.BundleVersion = string.Join(".", verArr);
		Logger.Log($"New BundleVersion: {BuildVersion.BundleVersion}");
		
		if (config.Versions.AndroidVersionCode != null)
		{
			var code = _workspace.GetAndroidBuildCode();
			BuildVersion.AndroidVersionCode = (code + 1).ToString();
		}

		if (config.Versions.BuildNumbers != null)
			BuildVersion.BuildNumbers = config.Versions.BuildNumbers;
	}
}