using System.Net;
using Builder;
using Builds.PreBuild;
using Deployment;
using Deployment.Configs;
using Deployment.RemoteBuild;
using Newtonsoft.Json;
using SharedLib;

namespace Server.RemoteBuild;

/// <summary>
/// Class send across network to do remote builds
/// </summary>
public class RemoteBuildTargetRequest : IRemoteControllable
{
	public string? SendBackUrl { get; init; }
	public OffloadServerPacket Packet { get; set; }

	[JsonIgnore] private string? WorkspaceName => Packet.WorkspaceName;
	[JsonIgnore] private int ChangesetId => Packet.ChangesetId;
	[JsonIgnore] private string? BuildVersion => Packet.BuildVersion;
	[JsonIgnore] private bool CleanBuild => Packet.CleanBuild;
	[JsonIgnore] private string[]? Links => Packet.Links;
	[JsonIgnore] private string[]? Copies => Packet.Copies;
	[JsonIgnore] private Dictionary<string, TargetConfig> BuildConfigs => Packet.Builds;
	

	/// <returns>BuildId</returns>
	public string Process()
	{
		ProcessAsync().FireAndForget();
		return "ok";
	}

	private async Task ProcessAsync()
	{
		// this needs to be here to kick start the thread, otherwise it will stall app
		await Task.Delay(1);
		
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		workspace.Clear();
		workspace.Update(ChangesetId);

		if (workspace.Directory == null || !Directory.Exists(workspace.Directory))
			throw new DirectoryNotFoundException($"Directory doesn't exist: {workspace.Directory}");
		
		await ClonesManager.CloneProject(workspaceName, Links, Copies, BuildConfigs.Values);

		foreach (var packet in BuildConfigs)
			StartBuilder(packet.Key, packet.Value, workspace, CleanBuild).FireAndForget();
	}

	/// <summary>
	/// Fire and forget method for starting a build
	/// </summary>
	/// <exception cref="WebException"></exception>
	private async Task StartBuilder(string buildId, TargetConfig config, Workspace workspace, bool clean)
	{
		try
		{
			Environment.CurrentDirectory = workspace.Directory;

			if (clean)
				workspace.CleanBuild();

			// pre build
			PreBuildBase.ReplaceVersions(BuildVersion);

			// build 
			var builder = new LocalUnityBuild(workspace.UnityVersion);
			var targetPath = ClonesManager.GetTargetPath(workspace.Directory, config);
			builder.Build(targetPath, config);

			RemoteBuildResponse response;

			if (builder.Errors is null)
			{
				// send web request back to sender with zip folder of build
				var zipBytes = await FilePacker.PackRawAsync(config.BuildPath);

				response = new RemoteBuildResponse
				{
					BuildId = buildId,
					BuildPath = config.BuildPath,
					Data = zipBytes
				};
			}
			else
			{
				// send web request to sender about the build failing
				response = BuildErrorResponse(buildId, config, builder.Errors);
			}

			await RespondBackToMasterServer(response);

			// clean up after build
			workspace.Clear();

			App.DumpLogs();
		}
		catch (Exception e)
		{
			Logger.Log(e);
			var res = BuildErrorResponse(buildId, config, e.Message);
			await RespondBackToMasterServer(res);
		}
	}

	private static RemoteBuildResponse BuildErrorResponse(string buildId, TargetConfig config, string? message = null)
	{
		return new RemoteBuildResponse
		{
			BuildId = buildId,
			BuildPath = config.BuildPath,
			Error = message ?? "build failed for reasons unknown"
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
}