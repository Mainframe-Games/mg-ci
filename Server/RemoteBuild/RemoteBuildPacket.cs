using SharedLib;
using SharedLib.Server;

namespace Server.RemoteBuild;

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
	/// For updating game servers
	/// </summary>
	public GameServerUpdate? GameServerUpdate { get; set; }
	
	/// <summary>
	/// Used to do any automation after switch `default` on Steam
	/// </summary>
	public ProductionRequest? ProductionProcess { get; set; }

	/// <summary>
	/// 
	/// </summary>
	/// <returns>Response back to web sender so they know what happened</returns>
	public ServerResponse Process()
	{
		if (WorkspaceBuildRequest != null) return WorkspaceBuildRequest.Process();
		if (BuildTargetRequest != null) return BuildTargetRequest.Process();
		if (BuildResponse != null) return BuildResponse.Process();
		if (GameServerUpdate != null) return GameServerUpdate.Process();
		if (ProductionProcess != null) return ProductionProcess.Process();
		throw new Exception($"Issue with {nameof(RemoteBuildPacket)}. {Json.Serialise(this)}");
	}
}