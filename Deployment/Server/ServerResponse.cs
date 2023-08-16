using System.Net;

namespace Deployment.Server;

public class ServerResponse
{
	public HttpStatusCode StatusCode { get; set; }
	public object? Data { get; set; }

	public ServerResponse(HttpStatusCode statusCode, object data)
	{
		StatusCode = statusCode;
		Data = data;
	}
	
	public static readonly ServerResponse Ok = new(HttpStatusCode.OK, "Ok");
	public static readonly ServerResponse UnAuthorised = new(HttpStatusCode.Unauthorized, "You are not authorized to perform this action");
	public static readonly ServerResponse NoContent = new(HttpStatusCode.NoContent, "No body was given in request");
}