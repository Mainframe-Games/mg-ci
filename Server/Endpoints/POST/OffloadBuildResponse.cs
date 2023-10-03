using System.Net;
using Deployment;
using Deployment.Configs;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace Server.Endpoints.POST;

/// <summary>
/// Response from offload server, used on master server
/// </summary>
public class OffloadBuildResponse : EndpointPOST<OffloadBuildResponse.Payload>
{
	public class Payload
	{
		public ulong PipelineId { get; set; }
		public string? BuildIdGuid { get; set; }
		public string? BuildName { get; set; }
		public BuildTaskStatus? Status { get; set; }
		public BuildResult? BuildResult { get; set; }
	}
	
	public override HttpMethod Method => HttpMethod.Post;
	public override string Path => "/offload-response";

	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, Payload content)
	{
		await Task.CompletedTask;
		
		if (!App.Pipelines.TryGetValue(content.PipelineId, out var buildPipeline))
			return LogAndReturn(new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildPipeline)} is not active. Id: {content.PipelineId}"));

		// if build name or buildGUID is null then errors could of happened before builds could even start
		if (content.BuildName == null || content.BuildIdGuid == null)
		{
			buildPipeline.SendErrorHook(new Exception(content.BuildResult?.Errors ?? "Unknown error. Something went wrong with offload server"));
			return ServerResponse.Ok;
		}

		if (content.BuildResult == null && content.Status is BuildTaskStatus.Succeed or BuildTaskStatus.Failed)
			return LogAndReturn(new ServerResponse(HttpStatusCode.BadRequest, $"{nameof(BuildResult)} can not be null"));

		buildPipeline.SetOffloadBuildStatus(content.BuildIdGuid, content.BuildName, content.Status ?? default, content.BuildResult);
		return ServerResponse.Ok;
	}

	private static ServerResponse LogAndReturn(ServerResponse res)
	{
		Logger.Log(res);
		return res;
	}
}