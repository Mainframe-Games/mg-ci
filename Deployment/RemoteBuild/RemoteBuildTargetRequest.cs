using System.Net;
using Deployment.Configs;
using Deployment.Misc;
using Deployment.PreBuild;
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
	public string? BuildVersion { get; init; }
	public bool CleanBuild { get; init; }
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
		StartBuilder(buildId).FireAndForget();
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
		
		if (CleanBuild)
			workspace.CleanBuild();
		
		// pre build
		PreBuildBase.ReplaceVersions(BuildVersion);
		
		// build 
		var builder = new LocalUnityBuild(workspace.UnityVersion);
		var success = await builder.Build(Config);

		RemoteBuildResponse response;
		
		if (success)
		{
			// send web request back to sender with zip folder of build
			var zipBytes = await FilePacker.PackRawAsync(Config.BuildPath);
			
			response = new RemoteBuildResponse
			{
				BuildId = buildId,
				BuildPath = Config?.BuildPath,
				Data = zipBytes
			};
		}
		else
		{
			// send web request to sender about the build failing
			response = BuildErrorResponse(buildId, builder.Errors);
		}
			
		await RespondBackToMasterServer(response);
		
		// clean up after build
		workspace.Clear();
	}

	private RemoteBuildResponse BuildErrorResponse(string buildId, string? message = null)
	{
		return new RemoteBuildResponse
		{
			BuildId = buildId,
			BuildPath = Config?.BuildPath,
			Error = message ?? "build failed for reasons"
		};
	}

	private async Task RespondBackToMasterServer(RemoteBuildResponse response)
	{
		Logger.Log($"Sending build '{response.BuildId}' back to: {SendBackUrl}");
		
		Web.Response res;
			
		if (string.IsNullOrEmpty(response.Error))
		{
			// success
			using var ms = new MemoryStream();
			await using var steam = new BinaryWriter(ms);
			response.Write(steam);
			res = await Web.SendBytesAsync(SendBackUrl, ms.ToArray());
		}
		else
		{
			// failed
			var body = new RemoteBuildPacket { BuildResponse = response };
			res =  await Web.SendAsync(HttpMethod.Post, SendBackUrl, body: body);
		}
		
		if (res.StatusCode != HttpStatusCode.OK)
			throw new WebException(res.Reason);
	}
}