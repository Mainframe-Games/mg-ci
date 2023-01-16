using System.Net;

namespace Deployment.Server;

public readonly struct ServerResponse
{
	public readonly HttpStatusCode StatusCode;
	public readonly string Message;

	public ServerResponse(HttpStatusCode statusCode, string message)
	{
		StatusCode = statusCode;
		Message = message;
	}
}