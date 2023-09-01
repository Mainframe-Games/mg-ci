using Deployment.Configs;
using Server.RemoteBuild;
using Server.RemoteDeploy;
using SharedLib;
using SharedLib.Build;

namespace Server;

public class Offloader : IOffloadable
{
	// interface
	public string? Url { get; set; }
	public List<BuildTargetFlag>? Targets { get; set; }
	public List<string> PendingIds { get; set; } = new();
	public OffloadServerPacket? Packet { get; set; }
	
	// class
	public string? SendBackUrl { get; set; }
	public string? WorkspaceName { get; set; }
	public string? WorkspaceBranch { get; set; }
	public ulong PipelineId { get; set; }
	public BuildConfig? BuildConfig { get; set; }

	public bool IsOffload(BuildSettingsAsset build, BuildVersions? buildVersion, int changesetId, bool isClean)
	{
		var flag = build.GetBuildTargetFlag();

		if (Targets?.Contains(flag) is not true)
			return false;
		
		Packet ??= new OffloadServerPacket
		{
			WorkspaceName = WorkspaceName,
			ChangesetId = changesetId,
			BuildVersion = buildVersion,
			PipelineId = PipelineId,
			CleanBuild = isClean,
			Branch = WorkspaceBranch,
			Builds = new Dictionary<string, OffloadBuildConfig>()
		};

		var buildId = CreateRemoteBuildTargetConfig(Packet, build, flag);
		PendingIds.Add(buildId);
		return true;
	}
	
	/// <summary>
	/// Returns once buildIds count is 0
	/// </summary>
	public async Task WaitBuildIdsAsync()
	{
		var cachedCount = -1;
		while (PendingIds.Count > 0)
		{
			// to limit the amount of log spamming just log when count changes
			if (PendingIds.Count != cachedCount)
			{
				Logger.Log($"Remaining buildIds: ({PendingIds.Count}) {string.Join(", ", PendingIds)}");
				cachedCount = PendingIds.Count;
			}
			
			await Task.Delay(TimeSpan.FromSeconds(10));
		}
	}

	/// <summary>
	/// Called from main build server. Sends web request to offload server and gets a buildId in return
	/// </summary>
	public async Task SendAsync()
	{
		var remoteBuild = new RemoteBuildTargetRequest
		{
			SendBackUrl = SendBackUrl,
			Packet = Packet
		};
	
		var body = new RemoteBuildPacket { BuildTargetRequest = remoteBuild };
		var res = await Web.SendAsync(HttpMethod.Post, Url, body: body);
		Logger.Log($"{nameof(Offloader)}: {res}");
	}
	
	private string CreateRemoteBuildTargetConfig(OffloadServerPacket packet, BuildSettingsAsset build, BuildTargetFlag flag)
	{
		var buildId = Guid.NewGuid().ToString();

		var buildConfig	= new OffloadBuildConfig { Name = build.Name, };
		
		if (flag is BuildTargetFlag.iOS && BuildConfig?.Deploy?.AppleStore is not true)
		{
			buildConfig.Deploy = new RemoteAppleDeploy
			{
				WorkspaceName = WorkspaceName,
				Config = new XcodeConfig
				{
					// TODO: work out a way to get id/pw here without too much mess
					AppleId = "",
					AppSpecificPassword = ""
				}
			};
		}

		packet.Builds[buildId] = buildConfig;
		
		return buildId;
	}
}