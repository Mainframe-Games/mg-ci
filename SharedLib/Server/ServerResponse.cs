using System.Net;

namespace SharedLib.Server;

public class ServerResponse
{
	public HttpStatusCode StatusCode { get; set; }
	public object? Data { get; set; }

	public ServerResponse(HttpStatusCode statusCode, object data)
	{
		StatusCode = statusCode;
		Data = data;
	}

	public override string ToString()
	{
		return $"{StatusCode}: {Data}";
	}

	public static readonly ServerResponse Ok = new(HttpStatusCode.OK, "Ok");
	public static readonly ServerResponse UnAuthorised = new(HttpStatusCode.Unauthorized, "You are not authorized to perform this action");
	public static readonly ServerResponse NotImplemented = new(HttpStatusCode.NotImplemented, "Action not implemented");
	public static readonly ServerResponse NoContent = new(HttpStatusCode.NoContent, "No body was given in request");
}