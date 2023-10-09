using System.Net;
using SharedLib;
using SharedLib.Server;

namespace Server.Endpoints;

public class CancelPipeline : Endpoint<CancelPipeline.Payload>
{
	public class Payload
	{
		public ulong PipelineId { get; set; }
	}
	
	public override string Path => "/cancel";

	protected override async Task<ServerResponse> DELETE()
	{
		await Task.CompletedTask;

		if (!App.Pipelines.TryGetValue(Content.PipelineId, out var pipeline))
			return new ServerResponse(HttpStatusCode.NotFound, $"Pipeline not found with Id: {Content.PipelineId}");
	
		Cmd.KillAll();
		pipeline.Cancel();
		App.Pipelines.Remove(Content.PipelineId);
	
		return new ServerResponse(HttpStatusCode.OK, "Build cancelled");
	}
}