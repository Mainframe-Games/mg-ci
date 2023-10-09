using System.Net;
using SharedLib.Server;

namespace Server.Endpoints;

public class TestServerConnection : Endpoint<object>
{
	public override string Path => "/test";

	protected override async Task<ServerResponse> GET()
	{
		await Task.CompletedTask;
		return ServerResponse.Ok;
	}

	protected override async Task<ServerResponse> HEAD()
	{
		await Task.CompletedTask;
		return new ServerResponse(HttpStatusCode.OK, null);
	}
}