using Deployment.Misc;
using Deployment.PreBuild;
using Deployment.Server.Config;
using Deployment.Server.Unity;
using SharedLib;

namespace Deployment.RemoteBuild;

public class ProductionRequest : IRemoteControllable
{
	public string? WorkspaceName { get; set; }

	public string Process()
	{
		var workspace = Workspace.GetWorkspaceFromName(WorkspaceName);
		Environment.CurrentDirectory = workspace.Directory;
		
		var buildVersion = PreBuildBase.GetAppVersion();

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
		var config = ServerConfig.Instance.UnityServices?.RemoteConfig;
		
		if (config == null)
			return;
		
		var unityRemoteConfig = new UnityRemoteConfigRequest(config.ConfigId);
		unityRemoteConfig.UpdateConfig(config.ValueKey, buildVersion).FireAndForget();
	}
}