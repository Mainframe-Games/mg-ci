namespace Builds;

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
	public void ReplaceVersions(string? newBundleVersion, string? androidVersionCode = null)
	{
		if (!WriteBundleVersion(newBundleVersion))
			throw new Exception("Failed to WriteBundleVersion");
		
		if (!WritePlatformBuildNumber(newBundleVersion, "Standalone"))
			throw new Exception("Failed to WritePlatformBuildNumber 'Standalone'");
		
		if (!WritePlatformBuildNumber(newBundleVersion, "iPhone"))
			throw new Exception("Failed to WritePlatformBuildNumber 'iPhone'");

		if (!string.IsNullOrEmpty(androidVersionCode))
		{
			if (!WriteAndroidBundleVersionCode(androidVersionCode))
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

	private bool WritePlatformBuildNumber(string? newBundleVersion, string platform)
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

	private bool WriteAndroidBundleVersionCode(string androidVersionCode)
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
	
	private static string ReplaceText(string line, string version)
	{
		var ver = line.Split(":").Last().Trim();
		var replacement = line.Replace(ver, version);
		return replacement;
	}
}