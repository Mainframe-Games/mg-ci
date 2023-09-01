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
		if (!DiscordWrapper.Instance.MessagesMap.TryGetValue(pipelineUpdateMessage.MessageId, out var message))
			return new ServerResponse(HttpStatusCode.NotFound, $"Count not find message with id: {pipelineUpdateMessage.MessageId}. Available: {string.Join(", ", DiscordWrapper.Instance.MessagesMap.Keys)}");

		if (pipelineUpdateMessage.Report is null)
			return new ServerResponse(HttpStatusCode.BadRequest, "Report can not be null");
		
		message.UpdateMessageAsync(pipelineUpdateMessage.Report).FireAndForget();
		return ServerResponse.Ok;
	}
}