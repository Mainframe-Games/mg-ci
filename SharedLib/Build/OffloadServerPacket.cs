namespace SharedLib.Build;

public class OffloadServerPacket
{
	public string? WorkspaceName { get; set; }
	public BuildVersions? BuildVersion { get; set; }
	public int ChangesetId { get; set; }
	public string? Branch { get; set; }
	public bool CleanBuild { get; set; }
	public ulong PipelineId { get; set; }

	/// <summary>
	/// BuildId (GUID), Asset Name <see cref="BuildSettingsAsset"/>
	/// </summary>
	public Dictionary<string, OffloadBuildConfig> Builds { get; set; } = new();
}

public class OffloadBuildConfig
{
	public string? Name { get; set; }
	public object? Deploy { get; set; }
}