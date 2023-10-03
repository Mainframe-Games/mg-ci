using System.Net;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints.GET;

/// <summary>
/// Used for making massive patch notes from specific changeset.
/// Good for Steam announcements
/// </summary>
public class Commits : Endpoint
{
	public override HttpMethod Method => HttpMethod.Get;
	public override string Path => "/commits";

	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		try
		{
			await Task.CompletedTask;
			var query = httpContext.Request.QueryString;
			var workspaceName = query["workspace"];
			var from = query["csfrom"];
			var to = query["csto"];
			var commits = LogToFileSteam(workspaceName, from, to);
			return new ServerResponse(HttpStatusCode.OK, commits);
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}
	
	private static string[] LogToFileSteam(string workspaceName, string csFrom, string csTo)
	{
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);

		if (workspace == null)
			throw new Exception($"Workspace not found with name '{workspaceName}'");
		
		var changeLog = workspace.GetChangeLogInst(int.Parse(csTo), int.Parse(csFrom), false);
		return changeLog;
	}
}