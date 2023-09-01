using System.Net;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace DiscordBot;

public class DiscordServerRequests : IProcessable
{
	public PipelineUpdateMessage? PipelineUpdate { get; set; }

	public ServerResponse Process()
	{
		if (PipelineUpdate is not null) return ProcessPipelineMessage(PipelineUpdate);
		return new ServerResponse(HttpStatusCode.BadRequest, $"Unable to process request. {Json.Serialise(this)}");
	}
	
	private static ServerResponse ProcessPipelineMessage(PipelineUpdateMessage pipelineUpdateMessage)
	{
		try
		{
			DiscordWrapper.Instance.ProcessUpdateMessage(pipelineUpdateMessage);
			return ServerResponse.Ok;
		}
		catch (Exception e)
		{
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}
}