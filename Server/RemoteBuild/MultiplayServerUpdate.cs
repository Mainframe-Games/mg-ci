using System.Net;
using SharedLib.Server;

namespace Server.RemoteBuild;

public class MultiplayServerUpdate : IProcessable
{
	public async Task<ServerResponse> ProcessAsync()
	{
		await Task.CompletedTask;
		return new ServerResponse(HttpStatusCode.NotImplemented, "😅");
	}
}