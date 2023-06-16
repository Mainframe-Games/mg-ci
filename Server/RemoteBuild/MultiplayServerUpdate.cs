using System.Net;
using Deployment.Server;

namespace Server.RemoteBuild;

public class MultiplayServerUpdate : IRemoteControllable
{
	public ServerResponse Process()
	{
		return new ServerResponse(HttpStatusCode.NotImplemented, "😅");
	}
}