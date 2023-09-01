using System.Net;
using Deployment;
using Deployment.Configs;
using Deployment.RemoteBuild;
using SharedLib;
using SharedLib.Server;

namespace Server.RemoteBuild;

/// <summary>
/// Class send across network to do remote builds
/// </summary>
public class RemoteBuildTargetRequest : IProcessable
{
	public string? SendBackUrl { get; set; }
	public OffloadServerPacket? Packet { get; set; }

	public ServerResponse Process()
	{
		if (Packet is null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(Packet)} can not be null");
		
		var workspaceName = new WorkspaceMapping().GetRemapping(Packet.WorkspaceName);
		var workspace = Workspace.GetWorkspaceFromName(workspaceName);
		
		if (workspace is null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Workspace not found: {Packet.WorkspaceName}");
		
		ProcessAsync(workspace).FireAndForget();
		return ServerResponse.Ok;
	}

	private async Task ProcessAsync(Workspace workspace)
	{
		// this needs to be here to kick start the thread, otherwise it will stall app
		await Task.Delay(1);

		Environment.CurrentDirectory = workspace.Directory;

		if (Packet.CleanBuild)
			workspace.CleanBuild();

		workspace.Clear();
		workspace.Update();
		workspace.SwitchBranch(Packet.Branch);
		workspace.Update(Packet.ChangesetId);

		if (!Directory.Exists(workspace.Directory))
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

		try
		{
			var builder = new LocalUnityBuild(workspace);
			var result = builder.Build(asset);

			if (!result.IsErrors)
				await Web.StreamToServerAsync(SendBackUrl, asset.BuildPath, pipelineId, buildIdGuid);

			await SendToMasterServerAsync(pipelineId, buildIdGuid, result.Errors, result);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await SendToMasterServerAsync(pipelineId, buildIdGuid, e.Message, null);
		}
	}

	private async Task SendToMasterServerAsync(ulong pipelineId, string? buildGuid, string? error,
		BuildResult? buildResult)
	{
		var response = new RemoteBuildResponse
		{
			PipelineId = pipelineId,
			BuildIdGuid = buildGuid,
			Error = error,
			BuildResult = buildResult
		};

		Logger.Log($"Sending build '{response.BuildIdGuid}' back to: {SendBackUrl}");
		var body = new RemoteBuildPacket { BuildResponse = response };
		await Web.SendAsync(HttpMethod.Post, SendBackUrl, body: body);
	}
}