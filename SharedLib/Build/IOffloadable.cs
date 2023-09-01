namespace SharedLib.Build;

public interface IOffloadable
{
	public string? Url { get; set; }

	List<BuildTargetFlag>? Targets { get; set; }
	List<string> PendingIds { get; set; }
	OffloadServerPacket? Packet { get; set; }

	/// <summary>
	/// Returns if offload is needed for IL2CPP
	/// <para></para>
	/// NOTE: Linux IL2CPP target can be built from Mac and Windows 
	/// </summary>
	/// <param name="build"></param>
	/// <param name="buildVersion"></param>
	/// <param name="changesetId"></param>
	/// <param name="isClean"></param>
	/// <returns></returns>
	bool IsOffload(BuildSettingsAsset build, BuildVersions? buildVersion, int changesetId, bool isClean);
	Task WaitBuildIdsAsync();
	Task SendAsync();
}