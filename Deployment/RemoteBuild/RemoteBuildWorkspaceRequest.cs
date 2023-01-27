namespace Deployment.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string[]? Args { get; set; }
	
	public async Task<string> ProcessAsync()
	{
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		
		var currentWorkspace = Workspace.GetWorkspaceFromName(workspaceName);
		Console.WriteLine($"Chosen workspace: {currentWorkspace}");

		if (BuildPipeline.Current != null)
			throw new Exception($"A build process already active. {BuildPipeline.Current.Workspace}");
		
		FireAndForgetBuild(currentWorkspace, Args);
		Console.WriteLine("Fired off build");
		await Task.CompletedTask;
		return $"{currentWorkspace.Name} | {currentWorkspace.UnityVersion}";
	}

	private static void FireAndForgetBuild(Workspace currentWorkspace, string[]? args)
	{
		var pipe = new BuildPipeline(currentWorkspace, args);
		Task.Run(pipe.RunAsync);
	}
}