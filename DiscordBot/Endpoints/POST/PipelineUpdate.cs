using System.Net;
using DiscordBot;
using SharedLib.Server;

namespace SharedLib.BuildToDiscord.POST;

public class PipelineUpdate : EndpointPOST<PipelineUpdate.Payload>
{
	public class Payload
	{
		public ulong CommandId { get; set; }
		public PipelineReport? Report { get; set; }
	}

	public override HttpMethod Method => HttpMethod.Post;
	public override string Path => "/pipeline-update";
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, Payload content)
	{
		try
		{
			await Task.CompletedTask;
			DiscordWrapper.Instance.ProcessUpdateMessage(content);
			return ServerResponse.Ok;
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}
}