using System.Net;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharedLib.Server;

namespace Server;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">Use object type for no body</typeparam>
public abstract class Endpoint<T> : IEndpoint, IProcessable<ListenServer, HttpListenerContext>
{
	/// <summary>
	/// Include / at start e.g `/endpoint`
	/// </summary>
	public abstract string Path { get; }
	protected ListenServer Server { get; private set; }
	protected HttpListenerContext HttpContext { get; private set; }

	/// <summary>
	/// Body content from POST etc...
	/// </summary>
	protected T? Content { get; private set; }

	public async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		Server = server;
		HttpContext = httpContext;
		
		var method = httpContext.Request.HttpMethod;

		// if contains body but doesn't deserialise properly
		if (!TryProcessBody(out var error))
			return error;
		
		return method switch
		{
			nameof(GET) => await GET(),
			nameof(POST) => await POST(),
			nameof(PUT) => await PUT(),
			nameof(HEAD) => await HEAD(),
			nameof(DELETE) => await DELETE(),
			nameof(PATCH) => await PATCH(),
			nameof(OPTIONS) => await OPTIONS(),
			_ => ServerResponse.NotImplemented
		};
	}

	protected virtual async Task<ServerResponse> GET()
	{
		await Task.CompletedTask;
		return ServerResponse.NotImplemented;
	}
	
	protected virtual async Task<ServerResponse> POST()
	{
		await Task.CompletedTask;
		return ServerResponse.NotImplemented;
	}
	
	protected virtual async Task<ServerResponse> PUT()
	{
		await Task.CompletedTask;
		return ServerResponse.NotImplemented;
	}
	
	protected virtual async Task<ServerResponse> HEAD()
	{
		await Task.CompletedTask;
		return new ServerResponse(HttpStatusCode.NotImplemented, null);
	}

	protected virtual async Task<ServerResponse> DELETE()
	{
		await Task.CompletedTask;
		return ServerResponse.NotImplemented;
	}
	
	protected virtual async Task<ServerResponse> PATCH()
	{
		await Task.CompletedTask;
		return ServerResponse.NotImplemented;
	}
	
	protected virtual async Task<ServerResponse> OPTIONS()
	{
		await Task.CompletedTask;
		return ServerResponse.NotImplemented;
	}
	
	public override string ToString()
	{
		return $"{Path} ({GetType().Name}.cs)";
	}

	private bool TryProcessBody(out ServerResponse? error)
	{
		error = null;
		
		// ignore types that dont have properties
		var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
		if (properties.Length == 0)
			return true;
		 
		Content = HttpContext.GetPostContent<T?>();

		if (Content is not null) 
			return true;
		
		// build schema
		var schema = new JObject();
		foreach (var propertyInfo in properties)
			schema[propertyInfo.Name] = propertyInfo.PropertyType.Name;
		
		var errorJson = new JObject
		{
			["Error"] = "Content is null",
			["Schema"] = schema
		};
		error = new ServerResponse(HttpStatusCode.BadRequest, errorJson);
		return false;

	}
}