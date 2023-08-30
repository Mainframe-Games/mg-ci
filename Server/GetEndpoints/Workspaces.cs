using System.Net;
using Server.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server;

public class Workspaces : IRemoteControllable
{
	public ServerResponse Process()
	{
		var workspaces = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
		return new ServerResponse(HttpStatusCode.OK, workspaces);
	}
}