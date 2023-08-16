using Deployment;
using Deployment.Server;

namespace Server.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public ulong PipelineId { get; set; }
	public string? BuildIdGuid { get; set; }
	public string? Error { get; set; }

	public ServerResponse Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (!App.Pipelines.TryGetValue(PipelineId, out var buildPipeline))
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active. Id: {PipelineId}");

		buildPipeline.OffloadBuildCompleted(BuildIdGuid);
		return ServerResponse.Ok;
	}
}