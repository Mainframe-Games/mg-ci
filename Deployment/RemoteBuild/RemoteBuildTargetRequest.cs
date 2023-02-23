using System.Net;
using Deployment.Configs;
using Deployment.Misc;
using Deployment.Server;
using SharedLib;

namespace Deployment.RemoteBuild;

/// <summary>
/// Class send across network to do remote builds
/// </summary>
public class RemoteBuildTargetRequest : IRemoteControllable
{
	/// <summary>
	/// Workspace name from the master server. Remapping is done on offload server
	/// </summary>
	public string? WorkspaceName { get; init; }
	public int ChangeSetId { get; init; }
	public string? SendBackUrl { get; init; }
	public TargetConfig? Config { get; init; }
	
	/// <summary>
	/// 
	/// </summary>
	/// <returns>BuildId</returns>
	public async Task<string> ProcessAsync()
	{
		var buildId = Guid.NewGuid().ToString();
		Logger.Log($"Created buildId: {buildId}");
		StartBuild(buildId);
		await Task.CompletedTask;
		return buildId;
	}

	private void StartBuild(string buildId)
	{
		Task.Run(() => StartBuilder(buildId)); // fire and forget
	}
	
	/// <summary>
	/// Fire and forget method for starting a build
	/// </summary>
	/// <param name="buildId"></param>
	/// <exception cref="WebException"></exception>
	private async Task StartBuilder(string buildId)
	{
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		workspace.Clear();
		workspace.Update(ChangeSetId);

		if (workspace.Directory == null || !Directory.Exists(workspace.Directory))
			throw new DirectoryNotFoundException($"Directory doesn't exist: {workspace.Directory}");

		if (Config == null)
		{
			await RespondBackToMasterServer(BuildErrorResponse(buildId, $"{nameof(TargetConfig)} is null"));
			return;
		}

		Environment.CurrentDirectory = workspace.Directory;
		
		var builder = new LocalUnityBuild(workspace.UnityVersion);
		var success = await builder.Build(Config);

		RemoteBuildResponse response;
		
		if (success)
		{
			// send web request back to sender with zip folder of build
			var base64 = await FilePacker.PackAsync(Config.BuildPath);
			response = new RemoteBuildResponse
			{
				Request = this,
				BuildId = buildId,
				Base64 = base64
			};
		}
		else
		{
			// send web request to sender about the build failing
			response = BuildErrorResponse(buildId, builder.Errors);
		}

		await RespondBackToMasterServer(response);
	}

	private RemoteBuildResponse BuildErrorResponse(string buildId, string? message = null)
	{
		return new RemoteBuildResponse
		{
			Request = this,
			BuildId = buildId,
			Error = message ?? "build failed for reasons"
		};
	}

	private static async Task RespondBackToMasterServer(RemoteBuildResponse response)
	{
		// build is done or failed, tell sender about it
		var sendBackUrl = response.Request.SendBackUrl;
		var body = new RemoteBuildPacket { BuildResponse = response };
		Logger.Log($"Sending build '{response.BuildId}' back to: {sendBackUrl}");
		var res =  await Web.SendAsync(HttpMethod.Post, sendBackUrl, body: body);
		if (res.StatusCode != HttpStatusCode.OK)
			throw new WebException(res.Reason);
	}
}