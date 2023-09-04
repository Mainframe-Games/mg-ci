using System.Net;
using Deployment;
using Deployment.Configs;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace Server.RemoteBuild;

public class RemoteBuildResponse : IProcessable
{
	public ulong PipelineId { get; set; }
	public string? BuildIdGuid { get; set; }
	public string? BuildName { get; set; }
	public BuildTaskStatus? Status { get; set; }
	public BuildResult? BuildResult { get; set; }

	public ServerResponse Process()
	{
		if (!App.Pipelines.TryGetValue(PipelineId, out var buildPipeline))
			return LogAndReturn(new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildPipeline)} is not active. Id: {PipelineId}"));

		// if build name or buildGUID is null then errors could of happened before builds could even start
		if (BuildName == null || BuildIdGuid == null)
		{
			buildPipeline.SendErrorHook(new Exception(BuildResult?.Errors ?? "Unknown error. Something went wrong with offload server"));
			return ServerResponse.Ok;
		}

		if (BuildResult == null && Status is BuildTaskStatus.Succeed or BuildTaskStatus.Failed)
			return LogAndReturn(new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildResult)} can not be null"));

		buildPipeline.SetOffloadBuildStatus(BuildIdGuid, BuildName, Status ?? default, BuildResult);
		return ServerResponse.Ok;
	}

	private static ServerResponse LogAndReturn(ServerResponse res)
	{
		Logger.Log(res);
		return res;
	}
}