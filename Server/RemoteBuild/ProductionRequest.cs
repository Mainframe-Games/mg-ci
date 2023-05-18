using Deployment.Configs;
using Deployment.Server;
using Deployment.Server.Unity;
using Server.Configs;
using SharedLib;

namespace Server.RemoteBuild;

public class ProductionRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }

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
		var clanforge = ServerConfig.Instance.Clanforge;
		
		if (clanforge == null)
			return;

		// get highest build version
		var pro = new RemoteClanforgeImageUpdate
		{
			Config = new ClanforgeConfig
			{
				AccessKey = clanforge.AccessKey,
				SecretKey = clanforge.SecretKey,
				Asid = clanforge.Asid,
				MachineId = clanforge.MachineId,
				Profiles = clanforge.Profiles,
				IsProduction = true
			},
			Desc = $"Build Version: {buildVersion}",
			Hooks = ServerConfig.Instance.Hooks
		};

		pro.Process();
	}

	private static void RemoteConfigProcess(string buildVersion)
	{
		if (ServerConfig.Instance.UnityServices == null)
			return;

		var unityServices = ServerConfig.Instance.UnityServices;
		var accessKey = unityServices.AccessKey;
		var secretKey = unityServices.SecretKey;
		var remoteConfig = unityServices.RemoteConfig;
		
		var unityRemoteConfig = new UnityRemoteConfigRequest(accessKey, secretKey, remoteConfig.ConfigId);
		unityRemoteConfig.OnUrlReq += unityServices.BuildUrl;
		unityRemoteConfig.UpdateConfig(remoteConfig.ValueKey, buildVersion).FireAndForget();
	}
}