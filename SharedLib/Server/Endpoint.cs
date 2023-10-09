using System.Net;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SharedLib.Server;

namespace Server;

public abstract class Endpoint<T> : IEndpoint, IProcessable<ListenServer, HttpListenerContext>
{
	/// <summary>
	/// Include / at start e.g `/endpoint`
	/// </summary>
	public abstract string Path { get; }
	protected ListenServer Server { get; private set; }
	protected HttpListenerContext HttpContext { get; private set; }

	/// <summary>
	/// Set to true if you want to ignore processing body in the base class
	/// </summary>
	protected virtual bool IgnoreBodyProcess => false;

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
		if (!IgnoreBodyProcess && !TryProcessBody(out var error))
			return error;
		
		try
		{
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
		catch (Exception e)
		{
			var errorJson = new JObject
			{
				["Exception"] = e.GetType().Name,
				["StackTrace"] = e.StackTrace
			};
			return new ServerResponse(HttpStatusCode.InternalServerError, errorJson);
		}
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
		return ServerResponse.NotImplemented;
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
		Content = HttpContext.GetPostContent<T?>();

		if (Content is null)
		{
			var errorJson = new JObject
			{
				["Error"] = "Content is null",
				["Schema"] = GetSchema(typeof(T))
			};
			
			error = new ServerResponse(HttpStatusCode.BadRequest, errorJson);
			return false;
		}

		error = null;
		return true;
	}

	private static JObject GetSchema(IReflect type)
	{
		var schema = new JObject();
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var propertyInfo in properties)
			schema[propertyInfo.Name] = propertyInfo.PropertyType.Name;
		return schema;
	}
}