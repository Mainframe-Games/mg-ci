using Deployment.Misc;
using SharedLib;

namespace Deployment.RemoteBuild;

public class RemoteBuildWorkspaceRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string[]? Args { get; set; }
	
	public string Process()
	{
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		Logger.Log($"Chosen workspace: {workspace}");
		workspace.Update();

		if (BuildPipeline.Current != null)
			throw new Exception($"A build process already active. {BuildPipeline.Current.Workspace}");
		
		var changeSetId = workspace.GetCurrentChangeSetId();
		
		var pipe = new BuildPipeline(workspace, Args);
		pipe.RunAsync().FireAndForget();
		
		return $"{workspace.Name} | {workspace.UnityVersion} | changeSetId: {changeSetId}";
	}
}