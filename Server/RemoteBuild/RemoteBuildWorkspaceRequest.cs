using System.Net;
using Deployment.RemoteBuild;
using Deployment.Server;
using SharedLib;

namespace Server.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string? Branch { get; set; } = "main";
	public string? Args { get; set; }
	
	public ServerResponse Process()
	{
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		Logger.Log($"Chosen workspace: {workspace}");
		workspace.Clear();
		workspace.Update();
		workspace.SwitchBranch(Branch);
		
		var argsArray = Args?.Split(' ');
		App.RunBuildPipe(workspace, argsArray).FireAndForget();
		workspace.GetCurrent(out var changeSetId, out var guid);

		return new ServerResponse
		{
			StatusCode = HttpStatusCode.OK,
			PipelineId = App.NextPipelineId - 1,
			Message = workspace.Name,
			UnityVersion = workspace.UnityVersion,
			ChangesetId = changeSetId,
			ChangesetGuid = guid,
			Branch = Branch
		};
	}
}