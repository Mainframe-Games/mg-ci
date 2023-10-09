using System.Collections.Specialized;
using System.Net;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints;

/// <summary>
/// Used for making massive patch notes from specific changeset.
/// Good for Steam announcements
/// </summary>
public class Commits : Endpoint<object>
{
	public override string Path => "/commits";

	private const string WORKSPACE = "workspace";
	private const string CS_FROM = "csfrom";
	private const string CS_TO = "csto";

	private static readonly string[] paramNames = {
		WORKSPACE,
		CS_FROM,
		CS_TO
	};

	protected override async Task<ServerResponse> GET()
	{
		try
		{
			await Task.CompletedTask;
			var query = HttpContext.Request.QueryString;

			if (!IsValid(query, out var error))
				return error;
			
			var workspaceName = query[WORKSPACE];
			var from = query[CS_FROM];
			var to = query[CS_TO];

			return LogToFileSteam(workspaceName, from, to);
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}

	private static bool IsValid(NameValueCollection query, out ServerResponse serverResponse)
	{
		foreach (var paramName in paramNames)
		{
			if (!string.IsNullOrEmpty(query[paramName]))
				continue;
			
			serverResponse = new ServerResponse(HttpStatusCode.BadRequest, $"Query must contain param '{paramName}'");
			return false;
		}

		serverResponse = ServerResponse.Ok;
		return true;
	}
	
	private static ServerResponse LogToFileSteam(string workspaceName, string csFrom, string csTo)
	{
		try
		{
			var workspace = Workspace.GetWorkspaceFromName(workspaceName);
			
			if (workspace == null)
				throw new Exception($"Workspace not found with name '{workspaceName}'");
			
			var commits = workspace.GetChangeLogInst(int.Parse(csTo), int.Parse(csFrom), false);
			return new ServerResponse(HttpStatusCode.OK, commits); 
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.BadRequest, e.Message);
		}
	}
}