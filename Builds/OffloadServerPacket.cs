using Deployment.Configs;
using SharedLib;

namespace Deployment;

public class OffloadServerPacket
{
	public string? WorkspaceName { get; set; }
	public BuildVersions? BuildVersion { get; set; }
	public int ChangesetId { get; set; }
	public string? Branch { get; set; }
	public bool CleanBuild { get; set; }
	public ParallelBuildConfig? ParallelBuild { get; set; }
	public ulong PipelineId { get; set; }
	
	/// <summary>
	/// BuildId (GUID), Asset Name <see cref="BuildSettingsAsset"/>
	/// </summary>
	public Dictionary<string, string> Builds { get; set; }
}