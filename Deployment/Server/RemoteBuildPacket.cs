using Deployment.RemoteBuild;

namespace Deployment.Server;

/// <summary>
/// Packet to be used for sending across web requests
/// </summary>
public class RemoteBuildPacket : IRemoteControllable
{
	/// <summary>
	/// Builds the entire workspace from buildconfig.json (used for master build server)
	/// </summary>
	public RemoteBuildWorkspaceRequest? WorkspaceBuildRequest { get; set; }
	
	/// <summary>
	/// Builds a specific target on an offload server
	/// </summary>
	public RemoteBuildTargetRequest? BuildTargetRequest { get; set; }
	
	/// <summary>
	/// Response from offload server, used on master server
	/// </summary>
	public RemoteBuildResponse? BuildResponse { get; set; }
	
	/// <summary>
	/// For updating a clanforge image via API request
	/// </summary>
	public RemoteClanforgeImageUpdate? ClanforgeImageUpdate { get; set; }

	/// <summary>
	/// 
	/// </summary>
	/// <returns>Response back to web sender so they know what happened</returns>
	public async Task<string> ProcessAsync()
	{
		if (WorkspaceBuildRequest != null) return await WorkspaceBuildRequest.ProcessAsync();
		if (BuildTargetRequest != null) return await BuildTargetRequest.ProcessAsync();
		if (BuildResponse != null) return await BuildResponse.ProcessAsync();
		if (ClanforgeImageUpdate != null) return await ClanforgeImageUpdate.ProcessAsync();
		throw new Exception($"Issue with {nameof(RemoteBuildPacket)}. Neither {nameof(BuildTargetRequest)} or {nameof(BuildResponse)} is valid");
	}
}