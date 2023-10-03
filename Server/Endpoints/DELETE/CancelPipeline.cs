using System.Net;
using SharedLib.Server;

namespace Server.Endpoints.DELETE;

public class CancelPipeline : Endpoint
{
	public override HttpMethod Method => HttpMethod.Delete;
	public override string Path => "/cancel";
	public override Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		throw new NotImplementedException();
	}
}