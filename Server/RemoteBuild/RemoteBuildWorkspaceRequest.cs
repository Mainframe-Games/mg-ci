using System.Net;
using Deployment.RemoteBuild;
using Deployment.Server;
using SharedLib;

namespace Server.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string? Args { get; set; }
	
	public ServerResponse Process()
	{
		var argsArray = Args?.Split(' ');
		var args = new Args(argsArray);
		args.TryGetArg("-branch", out var branch, "main");

		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		Logger.Log($"Chosen workspace: {workspace}");
		
		workspace.Clear();
		workspace.Update();
		workspace.SwitchBranch(branch);

		App.RunBuildPipe(workspace, args).FireAndForget();
		workspace.GetCurrent(out var changeSetId, out var guid);

		return new ServerResponse
		{
			StatusCode = HttpStatusCode.OK,
			PipelineId = App.NextPipelineId - 1,
			Message = workspace.Name,
			UnityVersion = workspace.UnityVersion,
			ChangesetId = changeSetId,
			ChangesetGuid = guid,
			Branch = branch
		};
	}
}