using System.Net;
using Builder;
using Deployment;
using Deployment.Configs;
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
		return ServerResponse.Default;
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

		if (Packet.ParallelBuild != null)
		{
			await ClonesManager.CloneProject(workspace.Directory, Packet.ParallelBuild.Links, Packet.ParallelBuild.Copies, Packet.Builds.Values);
			
			Packet.Builds
				.Select(x => Task.Run(() => StartBuilder(Packet.PipelineId, x.Key, x.Value, workspace, true)))
				.ToList()
				.WaitForAll();
		}
		else
		{
			foreach (var build in Packet.Builds)
				await StartBuilder(Packet.PipelineId, build.Key, build.Value, workspace, false);
		}
		
		// clean up after build
		workspace.Clear();
		App.DumpLogs();
	}

	/// <summary>
	/// Fire and forget method for starting a build
	/// </summary>
	/// <exception cref="WebException"></exception>
	private async Task StartBuilder(ulong pipelineId, string buildId, TargetConfig config, Workspace workspace, bool clone)
	{
		var originalBuildPath = config.BuildPath;
			
		try
		{
			var targetPath = clone
				? ClonesManager.GetTargetPath(workspace.Directory, config)
				: workspace.Directory;
			
			// build 
			var builder = new LocalUnityBuild(workspace.UnityVersion);
			config.BuildPath = Path.Combine(workspace.Directory, originalBuildPath); // Todo, consolidate this. Its in BuildPipeline.cs as well
			builder.Build(targetPath, config);

			RemoteBuildResponse response;

			if (builder.Errors is null)
			{
				// send web request back to sender with zip folder of build
				var zipBytes = await FilePacker.PackRawAsync(config.BuildPath);

				response = new RemoteBuildResponse
				{
					PipelineId = pipelineId,
					BuildId = buildId,
					BuildPath = originalBuildPath,
					Data = zipBytes
				};
			}
			else
			{
				// send web request to sender about the build failing
				response = BuildErrorResponse(pipelineId, buildId, originalBuildPath, builder.Errors);
			}

			await RespondBackToMasterServer(response);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			var res = BuildErrorResponse(pipelineId, buildId, originalBuildPath, e.Message);
			await RespondBackToMasterServer(res);
		}
	}

	private static RemoteBuildResponse BuildErrorResponse(ulong pipelineId, string? buildId, string? originalBuildPath, string? message)
	{
		return new RemoteBuildResponse
		{
			PipelineId = pipelineId,
			BuildId = buildId,
			BuildPath = originalBuildPath,
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