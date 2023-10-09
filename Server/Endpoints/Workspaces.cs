using System.Net;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints;

public class Workspaces : Endpoint<object>
{
	public override string Path => "/workspaces";

	protected override async Task<ServerResponse> GET()
	{
		await Task.CompletedTask;
		var workspaces = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
		return new ServerResponse(HttpStatusCode.OK, workspaces);
	}
}