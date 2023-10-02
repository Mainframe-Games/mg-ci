using System.Net;
using Deployment;
using Deployment.Server.Unity;
using Server.Configs;
using SharedLib;
using SharedLib.Server;

namespace Server.RemoteBuild;

public class ProductionRequest : IProcessable
{
	public string? WorkspaceName { get; set; }
	public string? Profile { get; set; }
	public string? Branch { get; set; }
	public string? Password { get; set; }

	public ServerResponse Process()
	{
		var workspace = Workspace.GetWorkspaceFromName(WorkspaceName);
		
		if (workspace == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Given namespace is not valid: {WorkspaceName}");
		
		workspace.Update();
		var buildVersion = workspace.GetFullVersion();

		if (buildVersion != Password)
			return new ServerResponse(HttpStatusCode.BadRequest, "Incorrect Password");

		Environment.CurrentDirectory = workspace.Directory;
		ClanforgeProcess(buildVersion);
		RemoteConfigProcess(buildVersion);
		return new ServerResponse(HttpStatusCode.OK, this);
	}

	private void ClanforgeProcess(string buildVersion)
	{
		// get highest build version
		var pro = new RemoteClanforgeImageUpdate
		{
			Profile = Profile,
			Beta = Branch,
			Desc = buildVersion
		};

		pro.Process();
	}

	private void RemoteConfigProcess(string buildVersion)
	{
		if (ServerConfig.Instance.Ugs == null)
			return;

		var unityServices = ServerConfig.Instance.Ugs;
		var accessKey = unityServices.KeyId;
		var secretKey = unityServices.SecretKey;
		var remoteConfig = unityServices.RemoteConfig;

		var project = ServerConfig.Instance.Ugs.GetProjectFromName(WorkspaceName);
		
		var unityRemoteConfig = new UnityRemoteConfigRequest(accessKey, secretKey, remoteConfig.ConfigId);
		unityRemoteConfig.UpdateConfig(project.ProjectId, remoteConfig.ValueKey, buildVersion).FireAndForget();
	}
}