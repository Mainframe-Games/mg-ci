using System.Net;
using Builds.PreBuild;
using Deployment;
using Deployment.Configs;
using Deployment.RemoteBuild;
using Newtonsoft.Json.Linq;
using Server.Configs;
using SharedLib;

namespace Server.RemoteBuild;

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

	/// <returns>BuildId</returns>
	public string Process()
	{
		var buildId = Guid.NewGuid().ToString();
		Logger.Log($"Created buildId: {buildId}");
		StartBuilder(buildId).FireAndForget();
		return buildId;
	}

	/// <summary>
	/// Fire and forget method for starting a build
	/// </summary>
	/// <param name="buildId"></param>
	/// <exception cref="WebException"></exception>
	private async Task StartBuilder(string buildId)
	{
		try
		{
			// this needs to be here to kick start the thread, otherwise it will stall app
			await Task.Delay(1);

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
			await builder.Build(Config);
			var success = builder.Errors is null;

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

			App.DumpLogs();
		}
		catch (Exception e)
		{
			Logger.Log(e);
			var res = BuildErrorResponse(buildId, e.Message);
			await RespondBackToMasterServer(res);
		}
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

		if (string.IsNullOrEmpty(response.Error))
		{
			// success
			using var ms = new MemoryStream();
			await using var steam = new BinaryWriter(ms);
			response.Write(steam);
			await Web.SendBytesAsync(SendBackUrl, ms.ToArray());
		}
		else
		{
			// failed
			var body = new RemoteBuildPacket { BuildResponse = response };
			await Web.SendAsync(HttpMethod.Post, SendBackUrl, body: body);
		}
	}
	
	/// <summary>
	/// Called from main build server. Sends web request to offload server and gets a buildId in return
	/// </summary>
	/// <param name="workspaceName"></param>
	/// <param name="changeSetId"></param>
	/// <param name="buildVersion"></param>
	/// <param name="targetConfig"></param>
	/// <param name="offloadUrl">The url to the offload server</param>
	/// <param name="cleanBuild"></param>
	/// <returns>Generated buildId</returns>
	/// <exception cref="WebException"></exception>
	public static async Task<string> SendRemoteBuildRequest(string? workspaceName, int changeSetId, string? buildVersion, TargetConfig targetConfig, string? offloadUrl, bool cleanBuild)
	{
		if (offloadUrl == null)
			throw new Exception("OffloadUrl is null");
		
		var remoteBuild = BuildRequest(workspaceName, changeSetId, buildVersion, targetConfig, cleanBuild);
		var body = new RemoteBuildPacket { BuildTargetRequest = remoteBuild };
		var res = await Web.SendAsync(HttpMethod.Post, offloadUrl, body: body);
		var json = JObject.Parse(res.Content);
		var buildId = json.SelectToken("Message", true)?.ToString() ?? string.Empty;
		Logger.Log($"Remote build id: {buildId}");
		return buildId;
	}
	
	private static RemoteBuildTargetRequest BuildRequest(string? workspaceName, int changeSetId, string? buildVersion, TargetConfig targetConfig, bool cleanBuild)
	{
		return new RemoteBuildTargetRequest
		{
			WorkspaceName = workspaceName,
			ChangeSetId = changeSetId,
			BuildVersion = buildVersion,
			Config = targetConfig,
			CleanBuild = cleanBuild,
			SendBackUrl = $"http://{ServerConfig.Instance.IP}:{ServerConfig.Instance.Port}"
		};
	}
}