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
		
		Logger.Log($"Environment.CurrentDirectory: {Environment.CurrentDirectory}");
		
		var currentWorkspace = Workspace.GetWorkspaceFromName(workspaceName);
		Logger.Log($"Chosen workspace: {currentWorkspace}");
		currentWorkspace.Update();

		if (BuildPipeline.Current != null)
			throw new Exception($"A build process already active. {BuildPipeline.Current.Workspace}");
		
		FireAndForgetBuild(currentWorkspace, Args);
		Logger.Log("Fired off build");
		await Task.CompletedTask;
		return $"{currentWorkspace.Name} | {currentWorkspace.UnityVersion}";
	}

	private static void FireAndForgetBuild(Workspace currentWorkspace, string[]? args)
	{
		var pipe = new BuildPipeline(currentWorkspace, args);
		Task.Run(pipe.RunAsync);
	}
}