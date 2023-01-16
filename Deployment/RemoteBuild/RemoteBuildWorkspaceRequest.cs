namespace Deployment.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string[]? Args { get; set; }
	
	public async Task<string> ProcessAsync()
	{
		var currentWorkspace = Workspace.GetWorkspaceFromName(WorkspaceName);
		Console.WriteLine($"Chosen workspace: {currentWorkspace}");

		if (BuildPipeline.Current != null)
			throw new Exception($"A build process already active. {BuildPipeline.Current.Workspace}");
		
		FireAndForgetBuild(currentWorkspace, Args);
		await Task.CompletedTask;
		return currentWorkspace.ToString();
	}

	private static async void FireAndForgetBuild(Workspace currentWorkspace, string[]? args)
	{
		var pipe = new BuildPipeline(currentWorkspace, args);
		await pipe.RunAsync();
		Console.WriteLine("FireAndForgetBuild finished");
	}
}