using Deployment.Server.Unity;
using Server.Configs;
using SharedLib;

namespace Server.RemoteBuild;

public class ProductionRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }

	public string Process()
	{
		var workspace = Workspace.GetWorkspaceFromName(WorkspaceName);
		workspace.Update();
		var buildVersion = workspace.GetAppVersion();

		Environment.CurrentDirectory = workspace.Directory;
		ClanforgeProcess(buildVersion);
		RemoteConfigProcess(buildVersion);
		return "ok";
	}

	private void ClanforgeProcess(string buildVersion)
	{
		var clanforge = ServerConfig.Instance.Clanforge;

		if (clanforge == null)
			return;
		
		// get highest build version
		
		var pro = new RemoteClanforgeImageUpdate
		{
			Config = clanforge,
			Desc = $"Build Version: {buildVersion}"
		};

		// set the image ids as production ids
		var productionIds = clanforge.ImageIdProfileNames
			?.Where(x => x.Value.Contains("Production"))
			.Select(x => uint.Parse(x.Key))
			.ToArray();

		pro.Config.ImageIds = productionIds;
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