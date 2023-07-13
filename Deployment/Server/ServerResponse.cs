using System.Net;

namespace Deployment.Server;

public class ServerResponse
{
	public static readonly ServerResponse Default = new();

	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
	public object? Data { get; set; } = "ok";

	public ServerResponse()
	{
	}

	public ServerResponse(HttpStatusCode statusCode, object data)
	{
		StatusCode = statusCode;
		Data = data;
	}
}