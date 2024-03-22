using System.Net;
using Deployment.Server.Unity;
using Server.Configs;
using Server.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints;

/// <summary>
/// Used to do any automation after switch `default` on Steam
/// </summary>
public class ProductionRequest : Endpoint<ProductionRequest.Payload>
{
	public class Payload
	{
		public string? WorkspaceName { get; set; }
		public string? Profile { get; set; }
		public string? Branch { get; set; }
		public string? Password { get; set; }
	}
	
	public override string Path => "/production";

	protected override async Task<ServerResponse> POST()
	{
		await Task.CompletedTask;
		
		var workspace = PlasticWorkspace.GetWorkspaceFromName(Content.WorkspaceName);
		
		if (workspace == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Given namespace is not valid: {Content.WorkspaceName}");
		
		workspace.Update();
		var buildVersion = workspace.GetFullVersion();

		if (buildVersion != Content.Password)
			return new ServerResponse(HttpStatusCode.BadRequest, "Incorrect Password");

		Environment.CurrentDirectory = workspace.Directory;
		await ClanforgeProcess(buildVersion);
        RemoteConfigProcess(buildVersion);
		return new ServerResponse(HttpStatusCode.OK, Content);
	}

	private async Task ClanforgeProcess(string buildVersion)
	{
		// get highest build version
		var pro = new ClanforgeImageUpdate
		{
			Profile = Content.Profile,
			Beta = Content.Branch,
			Desc = buildVersion,
			Full = false
		};

		await pro.ProcessAsync();
	}

	private void RemoteConfigProcess(string buildVersion)
	{
		if (ServerConfig.Instance.Ugs == null)
			return;

		var unityServices = ServerConfig.Instance.Ugs;
		var accessKey = unityServices.KeyId;
		var secretKey = unityServices.SecretKey;
		var remoteConfig = unityServices.RemoteConfig;

		var project = ServerConfig.Instance.Ugs.GetProjectFromName(Content.WorkspaceName);
		
		var unityRemoteConfig = new UnityRemoteConfigRequest(accessKey, secretKey, remoteConfig.ConfigId);
		unityRemoteConfig.UpdateConfig(project.ProjectId, remoteConfig.ValueKey, buildVersion).FireAndForget();
	}
}