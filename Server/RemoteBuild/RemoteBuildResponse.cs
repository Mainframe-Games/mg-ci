using Deployment;
using Deployment.Server;
using SharedLib;

namespace Server.RemoteBuild;

/// <summary>
/// This class will be massive, so it needs to be streamed to disk by default.
/// It can not remain in memory as it potentially could be over 10GB in size.
/// </summary>
public class RemoteBuildResponse : IRemoteControllable
{
	public ulong PipelineId { get; set; }
	public string? BuildIdGuid { get; set; }
	public string? BuildPath { get; set; }
	public string? Error { get; set; }

	public ServerResponse Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (!App.Pipelines.TryGetValue(PipelineId, out var buildPipeline))
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active. Id: {PipelineId}");
		
		// Logger.Log($"BuildId: {BuildIdGuid}, {Data?.ToByteSizeString()}");
		// buildPipeline.RemoteBuildReceived(BuildIdGuid, BuildPath, Data).FireAndForget();
		return ServerResponse.Ok;
	}
}