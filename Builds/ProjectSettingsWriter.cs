namespace Builds;

public class BuildVersions
{
	public string? BundleVersion { get; set; }
	public string? AndroidVersionCode { get; set; }
	public Dictionary<string, string>? BuildNumbers { get; set; }
}

public class ProjectSettingsWriter
{
	private readonly string? _path;
	private readonly string[] _lines;

	public ProjectSettingsWriter(string? path)
	{
		_path = path;
		_lines = File.ReadAllLines(path);
	}

	/// <summary>
	/// Replaces the version in all the places within ProjectSettings.asset
	/// </summary>
	public void ReplaceVersions(BuildVersions? buildVersions)
	{
		if (buildVersions == null)
			return;
		
		if (!WriteBundleVersion(buildVersions.BundleVersion))
			throw new Exception("Failed to WriteBundleVersion");

		if (buildVersions.BuildNumbers != null)
		{
			foreach (var buildNumber in buildVersions.BuildNumbers)
				if (!WritePlatformBuildNumber(buildNumber.Key, buildNumber.Value))
					throw new Exception($"Failed to WritePlatformBuildNumber '{buildNumber.Key}'");
		}

		if (!string.IsNullOrEmpty(buildVersions.AndroidVersionCode))
		{
			if (!WriteAndroidBundleVersionCode(buildVersions.AndroidVersionCode))
				throw new Exception("Failed to WriteAndroidBundleVersionCode");
		}
		
		File.WriteAllText(_path, string.Join("\n", _lines));
	}

	private bool WriteBundleVersion(string? newBundleVersion)
	{
		for (int i = 0; i < _lines.Length; i++)
		{
			// bundle version
			if (!_lines[i].Contains("bundleVersion:")) 
				continue;
			
			_lines[i] = ReplaceText(_lines[i], newBundleVersion);
			return true;
		}

		return false;
	}

	private bool WritePlatformBuildNumber(string platform, string? newBundleVersion)
	{
		var isBuildNumFound = false;

		for (int i = 0; i < _lines.Length; i++)
		{
			// build number
			if (!isBuildNumFound && _lines[i].Contains("buildNumber:"))
				isBuildNumFound = true;

			if (!isBuildNumFound)
				continue;

			if (_lines[i].Contains($"{platform}:"))
				continue;
			
			_lines[i] = ReplaceText(_lines[i], newBundleVersion);
			return true;
		}

		return false;
	}

	private bool WriteAndroidBundleVersionCode(string? androidVersionCode)
	{
		for (int i = 0; i < _lines.Length; i++)
		{
			if (_lines[i].Contains("AndroidBundleVersionCode:")) 
				continue;
			
			_lines[i] = ReplaceText(_lines[i], androidVersionCode);
			return true;
		}

		return false;
	}
	
	private static string ReplaceText(string? line, string? version)
	{
		var ver = line.Split(":").Last().Trim();
		var replacement = line.Replace(ver, version);
		return replacement;
	}
}