using System.Collections.Specialized;
using System.Net;
using Server.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server;

/// <summary>
/// Used for making massive patch notes from specific changeset.
/// Good for Steam announcements
/// </summary>
public class Commits : IRemoteControllable
{
	private readonly NameValueCollection _query;

	public Commits(NameValueCollection query)
	{
		_query = query;
	}

	private static string[] LogToFileSteam(string workspaceName, string csFrom, string csTo)
	{
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);

		if (workspace == null)
			throw new Exception($"Workspace not found with name '{workspaceName}'");
		
		var changeLog = workspace.GetChangeLogInst(int.Parse(csTo), int.Parse(csFrom), false);
		return changeLog;
	}
	
	public ServerResponse Process()
	{
		try
		{
			var workspaceName = _query["workspace"];
			var from = _query["csfrom"];
			var to = _query["csto"];
			var commits = LogToFileSteam(workspaceName, from, to);
			return new ServerResponse(HttpStatusCode.OK, commits);
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}
}