using System.Net;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints.GET;

public class Workspaces : Endpoint
{
	public override HttpMethod Method => HttpMethod.Get;
	public override string Path => "/workspaces";

	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		await Task.CompletedTask;
		var workspaces = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToList();
		return new ServerResponse(HttpStatusCode.OK, workspaces);
	}
}