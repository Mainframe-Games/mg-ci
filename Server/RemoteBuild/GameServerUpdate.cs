using SharedLib;
using SharedLib.Server;

namespace Server.RemoteBuild;

public class GameServerUpdate : IProcessable
{
	public MultiplayServerUpdate? Usg { get; set; }
	public RemoteClanforgeImageUpdate? Clanforge { get; set; }
	
	public ServerResponse Process()
	{
		if (Usg != null) return Usg.Process();
		if (Clanforge != null) return Clanforge.Process();
		throw new Exception($"Issue with {nameof(GameServerUpdate)}. {Json.Serialise(this)}");
	}
}