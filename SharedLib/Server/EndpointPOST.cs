using System.Net;
using Newtonsoft.Json.Linq;
using Server;

namespace SharedLib.Server;

public abstract class EndpointPOST<T> : Endpoint, IProcessable<ListenServer, HttpListenerContext, T> where T : class, new()
{
	protected T Content { get; private set; }
	
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		Content = await httpContext.GetPostContentAsync<T>();

		if (Content is null)
		{
			var error = new JObject
			{
				["Error"] = "Content is null",
				["Schema"] = JToken.FromObject(new T())
			};
			return new ServerResponse(HttpStatusCode.BadRequest, error);
		}
		
		return await ProcessAsync(server, httpContext, Content);
	}
	
	public abstract Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext, T content);
}