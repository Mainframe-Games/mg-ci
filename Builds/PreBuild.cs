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
		
		string? bundleVersion = null;
		
		if (config.Versions.BundleVersion != null)
		{
			var verArr = _workspace.GetVersionArray();
			bundleVersion = SetBundleVersion(verArr, config.BumpIndex);
		}
		
		if (config.Versions.AndroidVersionCode != null)
		{
			var code = _workspace.GetAndroidBuildCode();
			BundleAndroidBuildCode(code);
		}
		
		if (config.Versions.BuildNumbers != null)
			SetBuildVersionNumbers(config.Versions.BuildNumbers, bundleVersion);
	}

	private string SetBundleVersion(int[] verArr , int index)
	{
		verArr[index]++;
		var newVersion = string.Join(".", verArr);
		BuildVersion.BundleVersion = newVersion;
		return newVersion;
	}

	private void BundleAndroidBuildCode(int oldCode)
	{
		var newCode = oldCode + 1;
		BuildVersion.AndroidVersionCode = newCode.ToString();
	}

	private void SetBuildVersionNumbers(IEnumerable<string>? buildNumbers, string? bundleVersion)
	{
		BuildVersion.BuildNumbers ??= new();

		foreach (var buildNumber in buildNumbers)
			BuildVersion.BuildNumbers[buildNumber] = bundleVersion;
	}
}