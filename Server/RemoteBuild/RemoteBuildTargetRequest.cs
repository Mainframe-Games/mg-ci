using System.Net;
using Builder;
using Deployment;
using Deployment.RemoteBuild;
using Deployment.Server;
using SharedLib;

namespace Server.RemoteBuild;

/// <summary>
/// Class send across network to do remote builds
/// </summary>
public class RemoteBuildTargetRequest : IRemoteControllable
{
	public string? SendBackUrl { get; set; }
	public OffloadServerPacket? Packet { get; set; }

	public ServerResponse Process()
	{
		ProcessAsync().FireAndForget();
		return ServerResponse.Ok;
	}

	private async Task ProcessAsync()
	{
		// this needs to be here to kick start the thread, otherwise it will stall app
		await Task.Delay(1);
		
		var mapping = new WorkspaceMapping();
		var workspaceName = mapping.GetRemapping(Packet.WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		Environment.CurrentDirectory = workspace.Directory;
		
		if (Packet.CleanBuild)
			workspace.CleanBuild();
		
		workspace.Clear();
		workspace.Update();
		workspace.SwitchBranch(Packet.Branch);
		workspace.Update(Packet.ChangesetId);

		if (workspace.Directory == null || !Directory.Exists(workspace.Directory))
			throw new DirectoryNotFoundException($"Directory doesn't exist: {workspace.Directory}");
		
		// set build version in project settings
		var projWriter = new ProjectSettings(workspace.ProjectSettingsPath);
		projWriter.ReplaceVersions(Packet.BuildVersion);

		foreach (var build in Packet.Builds)
			await StartBuilder(Packet.PipelineId, build.Key, build.Value, workspace);
		
		// clean up after build
		workspace.Clear();
		App.DumpLogs();
	}

	/// <summary>
	/// Fire and forget method for starting a build
	/// </summary>
	/// <exception cref="WebException"></exception>
	private async Task StartBuilder(ulong pipelineId, string buildIdGuid, string buildTarget, Workspace workspace)
	{
		var asset = workspace.GetBuildTarget(buildTarget);
		var originalBuildPath = asset.BuildPath;
			
		try
		{
			// build
			var builder = new LocalUnityBuild(workspace);
			builder.Build(asset);

			if (builder.Errors is null)
			{
				// send web request back to sender with zip folder of build
				var res = new RemoteBuildResponse
				{
					PipelineId = pipelineId,
					BuildIdGuid = buildIdGuid,
					BuildPath = originalBuildPath
				};
				await Web.StreamToServerAsync(SendBackUrl, res.BuildPath, pipelineId, buildIdGuid);
			}
			else
			{
				// send web request to sender about the build failing
				await SendErrorToMasterServerAsync(pipelineId, buildIdGuid, originalBuildPath, builder.Errors);
			}

		}
		catch (Exception e)
		{
			Logger.Log(e);
			await SendErrorToMasterServerAsync(pipelineId, buildIdGuid, originalBuildPath, e.Message);
		}
	}

	private async Task SendErrorToMasterServerAsync(ulong pipelineId, string? buildId, string? originalBuildPath, string? message)
	{
		var response = new RemoteBuildResponse
		{
			PipelineId = pipelineId,
			BuildIdGuid = buildId,
			BuildPath = originalBuildPath,
			Error = message ?? "build failed for reasons unknown"
		};
		
		Logger.Log($"Sending build '{response.BuildIdGuid}' back to: {SendBackUrl}");
		var body = new RemoteBuildPacket { BuildResponse = response };
		await Web.SendAsync(HttpMethod.Post, SendBackUrl, body: body);
	}
}