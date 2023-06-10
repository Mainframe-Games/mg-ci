using Deployment.Server;
using Deployment.Server.Unity;
using Server.Configs;
using SharedLib;

namespace Server.RemoteBuild;

public class ProductionRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }
	public string? Profile { get; set; }
	public string? Branch { get; set; }

	public ServerResponse Process()
	{
		var workspace = Workspace.GetWorkspaceFromName(WorkspaceName);
		workspace.Update();
		var buildVersion = workspace.GetAppVersion();

		Environment.CurrentDirectory = workspace.Directory;
		ClanforgeProcess(buildVersion);
		RemoteConfigProcess(buildVersion);
		return ServerResponse.Default;
	}

	private void ClanforgeProcess(string buildVersion)
	{
		// get highest build version
		var pro = new RemoteClanforgeImageUpdate
		{
			Profile = Profile,
			Branch = Branch,
			Desc = $"Build Version: {buildVersion}"
		};

		pro.Process();
	}

	private void RemoteConfigProcess(string buildVersion)
	{
		if (ServerConfig.Instance.UnityServices == null)
			return;

		var unityServices = ServerConfig.Instance.UnityServices;
		var accessKey = unityServices.KeyId;
		var secretKey = unityServices.SecretKey;
		var remoteConfig = unityServices.RemoteConfig;

		var project = ServerConfig.Instance.UnityServices.GetProjectFromName(WorkspaceName);
		
		var unityRemoteConfig = new UnityRemoteConfigRequest(accessKey, secretKey, remoteConfig.ConfigId);
		unityRemoteConfig.UpdateConfig(project.ProjectId, remoteConfig.ValueKey, buildVersion).FireAndForget();
	}
}