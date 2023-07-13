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
		
		if (workspace == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Given namespace is not valid: {WorkspaceName}");
		
		Logger.Log($"Chosen workspace: {workspace}");
		
		workspace.Clear();
		workspace.Update();
		workspace.SwitchBranch(branch);

		var pipeline = App.CreateBuildPipeline(workspace, args);

		if (pipeline.ChangeLog.Length == 0)
			return new ServerResponse(HttpStatusCode.NotAcceptable, "No changes to build");
		
		App.RunBuildPipe(pipeline).FireAndForget();
		workspace.GetCurrent(out var changeSetId, out var guid);

		return new ServerResponse
		{
			StatusCode = HttpStatusCode.OK,
			Data = new BuildPipelineResponse
			{
				ServerVersion = App.Version,
				PipelineId = pipeline.Id,
				Workspace = workspace.Name,
				Args = Args,
				UnityVersion = workspace.UnityVersion,
				ChangesetId = changeSetId,
				ChangesetGuid = guid,
				Branch = branch,
				ChangesetCount = pipeline.ChangeLog.Length,
			}
		};
	}
}

public class BuildPipelineResponse
{
	public string? ServerVersion { get; set; }
	public ulong? PipelineId { get; set; }
	public string? Workspace { get; set; }
	public string? Args { get; set; }
	public string? Branch { get; set; }
	public int? ChangesetId { get; set; }
	public string? ChangesetGuid { get; set; }
	public string? UnityVersion { get; set; }
	public int? ChangesetCount { get; set; }
}