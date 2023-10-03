using System.Net;
using Server;

namespace SharedLib.Server;

public abstract class EndpointPOST<T> : Endpoint, IProcessable<ListenServer, HttpListenerContext, T>
{
	protected T Content { get; private set; }
	
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		if (!httpContext.Request.HasEntityBody) return ServerResponse.NoContent;
		Content = await httpContext.GetPostContentAsync<T>();
		return await ProcessAsync(server, httpContext, Content);
	}
	
	public abstract Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, T content);
}