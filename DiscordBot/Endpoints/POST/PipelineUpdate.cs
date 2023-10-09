using System.Net;
using DiscordBot;
using Server;
using SharedLib.Server;

namespace SharedLib.BuildToDiscord.POST;

public class PipelineUpdate : Endpoint<PipelineUpdate.Payload>
{
	public class Payload
	{
		public ulong CommandId { get; set; }
		public PipelineReport? Report { get; set; }
	}

	public override string Path => "/pipeline-update";

	protected override async Task<ServerResponse> POST()
	{
		try
		{
			await Task.CompletedTask;
			DiscordWrapper.Instance.ProcessUpdateMessage(Content);
			return ServerResponse.Ok;
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}
}