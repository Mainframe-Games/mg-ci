using System.Net;
using Deployment;
using Deployment.Configs;
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
			return new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildPipeline)} is not active. Id: {PipelineId}");

		if (BuildName == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildName)} can not be null");
		
		if (BuildIdGuid == null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildIdGuid)} can not be null");
		
		if (BuildResult == null && Status is BuildTaskStatus.Succeed or BuildTaskStatus.Failed) 
			return new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildResult)} can not be null");

		buildPipeline.SetOffloadBuildStatus(BuildIdGuid, BuildName, Status ?? default, BuildResult);
		return ServerResponse.Ok;
	}
}