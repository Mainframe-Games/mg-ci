namespace SharedLib;

public class BuildVersions
{
	public string? BundleVersion { get; set; }
	public string? AndroidVersionCode { get; set; }
	public Dictionary<string, string>? BuildNumbers { get; set; }
}

public class ProjectSettings
{
	private readonly string? _path;
	private readonly string[] _lines;

	public ProjectSettings(string? path)
	{
		_path = path;
		_lines = File.ReadAllLines(path);
	}

	#region Reads

	public string? GetProjPropertyValue(params string[] propertyNames)
	{
		var index = GetProjPropertyLineIndex(propertyNames);
		return _lines[index].Replace($"{propertyNames[^1]}:", string.Empty).Trim();
	}
	
	public int GetProjPropertyLineIndex(params string[] propertyNames)
	{
		var index = 0;

		for (var i = 0; i < _lines.Length; i++)
		{
			var line = _lines[i];
			var propName = $"{propertyNames[index]}:";

			// last index, return value
			if (index == propertyNames.Length - 1 && _lines[i].Contains(propName))
				return i;

			// increase index when prop found
			if (line.Contains(propName))
				index++;
		}

		throw new KeyNotFoundException($"Failed to find path: '{string.Join(", ", propertyNames)}'");
	}

	#endregion

	#region Writes

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
				WritePlatformBuildNumber(buildNumber.Key, buildNumber.Value);

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
	
	private static string ReplaceText(string? line, string? newValue)
	{
		if (line == null)
			throw new ArgumentNullException($"{nameof(line)} params is null");
		
		var oldValue = line.Split(":").Last().Trim();

		if (string.IsNullOrEmpty(oldValue))
			throw new NullReferenceException($"{nameof(oldValue)} is null on line '{line}'");
		
		var replacement = line.Replace(oldValue, newValue);
		return replacement;
	}
	
	#endregion
}