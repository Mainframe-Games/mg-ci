namespace SharedLib;

public class BuildVersions
{
	public string? BundleVersion { get; set; }
	public string? AndroidVersionCode { get; set; }
	public string[]? BuildNumbers { get; set; }
}

public class ProjectSettings : Yaml
{
	public ProjectSettings(string? path) : base(path)
	{
	}

	/// <summary>
	/// Replaces the version in all the places within ProjectSettings.asset
	/// </summary>
	public void ReplaceVersions(BuildVersions? buildVersions)
	{
		if (buildVersions == null)
			return;

		WriteBundleVersion(buildVersions.BundleVersion);

		if (buildVersions.BuildNumbers != null)
			foreach (var buildNumber in buildVersions.BuildNumbers)
				WritePlatformBuildNumber(buildNumber, buildVersions.BundleVersion);

		if (!string.IsNullOrEmpty(buildVersions.AndroidVersionCode))
			WriteAndroidBundleVersionCode(buildVersions.AndroidVersionCode);
		
		File.WriteAllText(_path, string.Join("\n", _lines));
	}

	private void WriteBundleVersion(string? newBundleVersion)
	{
		var index = GetProjPropertyLineIndex("bundleVersion");
		_lines[index] = ReplaceText(_lines[index], newBundleVersion);
	}

	private void WritePlatformBuildNumber(string platform, string? newBundleVersion)
	{
		var index = GetProjPropertyLineIndex("buildNumber", platform);
		_lines[index] = ReplaceText(_lines[index], newBundleVersion);
	}

	private void WriteAndroidBundleVersionCode(string? androidVersionCode)
	{
		var index = GetProjPropertyLineIndex("AndroidBundleVersionCode");
		_lines[index] = ReplaceText(_lines[index], androidVersionCode);
	}
}