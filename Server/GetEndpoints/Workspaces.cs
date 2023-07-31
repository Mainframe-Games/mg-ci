using System.Net;
using Deployment.Server;
using Server.RemoteBuild;
using SharedLib;

namespace Server;

public class Workspaces : IRemoteControllable
{
	public ServerResponse Process()
	{
		var workspaces = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
		return new ServerResponse(HttpStatusCode.OK, workspaces);
	}
}