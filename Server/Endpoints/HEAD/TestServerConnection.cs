using System.Net;
using SharedLib.Server;

namespace Server.Endpoints.HEAD;

public class TestServerConnection : Endpoint
{
	public override HttpMethod Method => HttpMethod.Head;
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		await Task.CompletedTask;
		return new ServerResponse(HttpStatusCode.OK, null);
	}
}