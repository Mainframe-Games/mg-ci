using System.Net;
using Newtonsoft.Json.Linq;
using Server;

namespace SharedLib.Server;


/// <summary>
/// Extension of Endpoint but with content body handled in a nice way
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class EndpointBody<T> : Endpoint, IProcessable<ListenServer, HttpListenerContext, T> where T : class, new()
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