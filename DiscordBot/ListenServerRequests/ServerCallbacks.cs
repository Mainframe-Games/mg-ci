using System.Net;
using SharedLib.Server;

namespace DiscordBot;

public class ServerCallbacks : IServerCallbacks
{
	public ListenServer Server { get; set; }
	
	public Task<ServerResponse> Get(HttpListenerContext context)
	{
		throw new NotImplementedException();
	}

	public async Task<ServerResponse> Post(HttpListenerContext context)
	{
		if (!context.Request.HasEntityBody) return ServerResponse.NoContent;
		
		var packet = await context.GetPostContentAsync<ServerRequests>();
		return packet.Process();
	}

	public Task<ServerResponse> Put(HttpListenerContext context)
	{
		throw new NotImplementedException();
	}
}