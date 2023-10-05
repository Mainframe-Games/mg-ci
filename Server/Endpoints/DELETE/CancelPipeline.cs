using System.Net;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints.DELETE;

public class CancelPipeline : EndpointBody<CancelPipeline.Payload>
{
	public class Payload
	{
		public ulong PipelineId { get; set; }
	}
	
	public override HttpMethod Method => HttpMethod.Delete;
	public override string Path => "/cancel";

	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, Payload content)
	{
		await Task.CompletedTask;

		if (!App.Pipelines.TryGetValue(content.PipelineId, out var pipeline))
			return new ServerResponse(HttpStatusCode.NotFound, $"Pipeline not found with Id: {content.PipelineId}");
	
		Cmd.KillAll();
		pipeline.Cancel();
		App.Pipelines.Remove(content.PipelineId);
	
		return new ServerResponse(HttpStatusCode.OK, "Build cancelled");
	}
}