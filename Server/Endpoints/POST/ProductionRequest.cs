using System.Net;
using Deployment.Server.Unity;
using Server.Configs;
using Server.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints.POST;

/// <summary>
/// Used to do any automation after switch `default` on Steam
/// </summary>
public class ProductionRequest : EndpointPOST<ProductionRequest.Payload>
{
	public class Payload
	{
		public string? WorkspaceName { get; set; }
		public string? Profile { get; set; }
		public string? Branch { get; set; }
		public string? Password { get; set; }
	}
	
	public override HttpMethod Method => HttpMethod.Post;
	public override string Path => "/production";
	
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, Payload content)
	{
		await Task.CompletedTask;
		
		var workspace = Workspace.GetWorkspaceFromName(content.WorkspaceName);
		
		if (workspace == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Given namespace is not valid: {content.WorkspaceName}");
		
		workspace.Update();
		var buildVersion = workspace.GetFullVersion();

		if (buildVersion != content.Password)
			return new ServerResponse(HttpStatusCode.BadRequest, "Incorrect Password");

		Environment.CurrentDirectory = workspace.Directory;
		await ClanforgeProcess(buildVersion);
		RemoteConfigProcess(buildVersion);
		return new ServerResponse(HttpStatusCode.OK, this);
	}

	private async Task ClanforgeProcess(string buildVersion)
	{
		// get highest build version
		var pro = new RemoteClanforgeImageUpdate
		{
			Profile = Content.Profile,
			Beta = Content.Branch,
			Desc = buildVersion
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