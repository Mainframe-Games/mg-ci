using System.Net;
using Deployment;
using Deployment.Configs;
using Deployment.RemoteBuild;
using Server.RemoteDeploy;
using SharedLib;
using SharedLib.Build;
using SharedLib.BuildToDiscord;
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
		{
			var asset = workspace.GetBuildTarget(build.Value.AssetName);
			var deployProcess = GetDeployProcess(build.Value, asset);
			await StartBuilderAsync(Packet.PipelineId, build.Key, asset, workspace, deployProcess);
		}

		// clean up after build
		workspace.Clear();
		App.DumpLogs();
	}
	
	/// <summary>
	/// Returns IProcessable for remote deploy if there is one
	/// </summary>
	/// <param name="build"></param>
	/// <param name="asset"></param>
	/// <returns></returns>
	private static IProcessable? GetDeployProcess(OffloadBuildConfig build, BuildSettingsAsset asset)
	{
		if (build.Deploy is null)
			return null;
		
		var deployStr = Json.Serialise(build.Deploy);
		
		if (asset.Target is BuildTarget.iOS)
			return Json.Deserialise<RemoteAppleDeploy>(deployStr);

		return null;
	}

	/// <summary>
	/// Fire and forget method for starting a build
	/// </summary>
	/// <exception cref="WebException"></exception>
	private async Task StartBuilderAsync(ulong pipelineId, string buildIdGuid, BuildSettingsAsset asset, Workspace workspace, IProcessable? deployProcess = null)
	{
		try
		{
			var builder = new LocalUnityBuild(workspace);
			await SendToMasterServerAsync(pipelineId, buildIdGuid, asset.Name, null, BuildTaskStatus.Pending);
			var result = builder.Build(asset);

			if (result.IsErrors)
			{
				await SendToMasterServerAsync(pipelineId, buildIdGuid, asset.Name, result, BuildTaskStatus.Failed);
				return;
			}
			
			// if no deploy then master server does deploy, need to upload this build
			if (deployProcess is null)
				await Web.StreamToServerAsync(SendBackUrl, asset.BuildPath, pipelineId, buildIdGuid);
			else
				deployProcess.Process();
			
			// tell master server build is completed
			await SendToMasterServerAsync(pipelineId, buildIdGuid, asset.Name, result, result.IsErrors ? BuildTaskStatus.Failed : BuildTaskStatus.Succeed);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			var result = new BuildResult
			{
				BuildName = asset.Name,
				Errors = $"{e.GetType().Name}: {e.Message}"
			};
			await SendToMasterServerAsync(pipelineId, buildIdGuid, asset.Name, result, BuildTaskStatus.Failed);
		}
	}

	private async Task SendToMasterServerAsync(ulong pipelineId, string? buildGuid, string buildName, BuildResult? buildResult, BuildTaskStatus status)
	{
		var response = new RemoteBuildResponse
		{
			PipelineId = pipelineId,
			BuildIdGuid = buildGuid,
			BuildName = buildName,
			BuildResult = buildResult,
			Status = status
		};

		Logger.Log($"Sending build '{response.BuildIdGuid}' back to: {SendBackUrl}");
		var body = new RemoteBuildPacket { BuildResponse = response };
		await Web.SendAsync(HttpMethod.Post, SendBackUrl, body: body);
	}
}