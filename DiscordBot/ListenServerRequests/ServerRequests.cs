using System.Net;
using SharedLib;
using SharedLib.BuildToDiscord;
using SharedLib.Server;

namespace DiscordBot;

public class ServerRequests
{
	public PipelineUpdateMessage? PipelineUpdate { get; set; }
	
	public ServerResponse Process()
	{
		if (PipelineUpdate is not null) return ProcessPipelineMessage(PipelineUpdate);
		throw new Exception($"Unable to process request. {Json.Serialise(this)}");
	}
	
	private static ServerResponse ProcessPipelineMessage(PipelineUpdateMessage pipelineUpdateMessage)
	{
		if (!DiscordWrapper.Instance.TryGetMessage(pipelineUpdateMessage.MessageId, out var message))
			return new ServerResponse(HttpStatusCode.NotFound, $"Count not find message with id: {pipelineUpdateMessage.MessageId}");

		if (pipelineUpdateMessage.Report is null)
			return new ServerResponse(HttpStatusCode.BadRequest, "Report can not be null");
		
		message.UpdateMessageAsync(pipelineUpdateMessage.Report).FireAndForget();
		return ServerResponse.Ok;
	}
}