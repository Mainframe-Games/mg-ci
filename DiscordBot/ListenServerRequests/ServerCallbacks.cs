using System.Net;
using SharedLib.Server;

namespace DiscordBot;

public class ServerCallbacks : IServerCallbacks
{
	public ListenServer Server { get; set; }
	
	public async Task<ServerResponse> Get(HttpListenerContext context)
	{
		await Task.CompletedTask;
		return ServerResponse.Ok;
	}

	public async Task<ServerResponse> Post(HttpListenerContext context)
	{
		if (!context.Request.HasEntityBody) return ServerResponse.NoContent;
		
		var packet = await context.GetPostContentAsync<DiscordServerRequests>();
		return packet.Process();
	}

	public Task<ServerResponse> Put(HttpListenerContext context)
	{
		throw new NotImplementedException();
	}
}