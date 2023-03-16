using SharedLib;

namespace Deployment.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string[]? Args { get; set; }
	
	public async Task<string> ProcessAsync()
	{
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		Logger.Log($"Chosen workspace: {workspace}");
		workspace.Update();

		if (BuildPipeline.Current != null)
			throw new Exception($"A build process already active. {BuildPipeline.Current.Workspace}");
		
		var changeSetId = workspace.GetCurrentChangeSetId();
		FireAndForgetBuild(workspace, Args);
		
		await Task.CompletedTask;
		return $"{workspace.Name} | {workspace.UnityVersion} | changeSetId: {changeSetId}";
	}

	private static void FireAndForgetBuild(Workspace currentWorkspace, string[]? args)
	{
		var pipe = new BuildPipeline(currentWorkspace, args);
		Task.Run(pipe.RunAsync);
	}
}