using System.Net;
using Server;

namespace SharedLib.Server;

public abstract class EndpointPOST<T> : Endpoint, IProcessable<ListenServer, HttpListenerContext, T> where T : class, new()
{
	protected T Content { get; private set; }
	
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		if (!httpContext.Request.HasEntityBody) return ServerResponse.NoContent;
		Content = await httpContext.GetPostContentAsync<T>();

		if (Content is null)
			return new ServerResponse(HttpStatusCode.BadRequest, $"Expected JSON Schema {Json.Serialise(new T())}");
		
		return await ProcessAsync(server, httpContext, Content);
	}
	
	public abstract Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, T content);
}