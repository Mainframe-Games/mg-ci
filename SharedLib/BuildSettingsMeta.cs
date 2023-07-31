namespace SharedLib;

public class BuildSettingsMeta : Yaml
{
	public string? Path { get; }
	
	public BuildSettingsMeta(string? path, int skip = 0) : base(path, skip)
	{
		Path = path;
	}

	public BuildSettingsAsset GetParentFile()
	{
		var filePath = Path?.Replace(".meta", string.Empty);
		return new BuildSettingsAsset(filePath);
	}
}