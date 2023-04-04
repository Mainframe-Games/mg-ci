using Deployment;
using Deployment.Configs;
using Deployment.Deployments;
using Deployment.RemoteBuild;
using Server.Configs;
using SharedLib;

namespace Server.RemoteBuild;

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
		Run(workspace).FireAndForget();
		return $"{workspace.Name} | {workspace.UnityVersion} | changeSetId: {changeSetId}";
	}

	private async Task Run(Workspace workspace)
	{
		var buildPipeline = new BuildPipeline(workspace, Args, ServerConfig.Instance.OffloadServerUrl);
		buildPipeline.OffloadBuildNeeded += RemoteBuildTargetRequest.SendRemoteBuildRequest;
		buildPipeline.GetExtraHookLogs += BuildPipelineOnGetExtraHookLog;
		buildPipeline.DeployEvent += BuildPipelineOnDeployEvent;
		await buildPipeline.RunAsync();
		App.DumpLogs();
	}

	private async Task BuildPipelineOnDeployEvent(DeployContainer deploy, string buildversiontitle)
	{
		// steam
		if (deploy.Steam != null)
		{
			foreach (var vdfPath in deploy.Steam)
			{
				var path = ServerConfig.Instance.Steam.Password;
				var password = ServerConfig.Instance.Steam.Password;
				var username = ServerConfig.Instance.Steam.Username;
				var steam = new SteamDeploy(vdfPath, password, username, path);
				steam.Deploy(buildversiontitle);
			}
		}

		// clanforge
		if (deploy.Clanforge == true)
		{
			var clanforge = new ClanForgeDeploy(ServerConfig.Instance.Clanforge, buildversiontitle);
			await clanforge.Deploy();
		}
	}

	private static string? BuildPipelineOnGetExtraHookLog()
	{
		return ServerConfig.Instance.Clanforge?.BuildHookMessage("Updated");
	}
}