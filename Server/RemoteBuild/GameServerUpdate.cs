using Deployment.Server;
using SharedLib;

namespace Server.RemoteBuild;

public class GameServerUpdate : IRemoteControllable
{
	public RemoteClanforgeImageUpdate? Usg { get; set; }
	public RemoteClanforgeImageUpdate? Clanforge { get; set; }
	
	public ServerResponse Process()
	{
		if (Usg != null) return Usg.Process();
		if (Clanforge != null) return Clanforge.Process();
		throw new Exception($"Issue with {nameof(GameServerUpdate)}. {Json.Serialise(this)}");
	}
}