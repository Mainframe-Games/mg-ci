using System.Net;
using Deployment.Server;
using Server.RemoteBuild;
using SharedLib;

namespace Server;

public class Workspaces : IRemoteControllable
{
	public ServerResponse Process()
	{
		return new ServerResponse(HttpStatusCode.OK, WorkspacePacket.GetFromLocal());
	}
}