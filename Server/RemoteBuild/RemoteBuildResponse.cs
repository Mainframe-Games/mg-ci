using Deployment;
using Deployment.Configs;
using SharedLib.Server;

namespace Server.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public ulong PipelineId { get; set; }
	public string? BuildIdGuid { get; set; }
	public string? Error { get; set; }
	public BuildResult? BuildResult { get; set; }

	public ServerResponse Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (!App.Pipelines.TryGetValue(PipelineId, out var buildPipeline))
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active. Id: {PipelineId}");
		if (BuildIdGuid == null)
			throw new NullReferenceException($"{nameof(BuildIdGuid)} can not be null");
		if (BuildResult == null)
			throw new NullReferenceException($"{nameof(BuildResult)} can not be null");

		buildPipeline.OffloadBuildCompleted(BuildIdGuid, BuildResult);
		return ServerResponse.Ok;
	}
}