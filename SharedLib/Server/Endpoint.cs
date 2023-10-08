using System.Net;
using System.Reflection;
using SharedLib.Server;

namespace Server;

public abstract class Endpoint : IProcessable<ListenServer, HttpListenerContext>
{
	public abstract HttpMethod Method { get; }
	
	/// <summary>
	/// Include / at start e.g `/endpoint`
	/// </summary>
	public virtual string Path => "/";

	public abstract Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext);

	public override string ToString()
	{
		return $"{GetType().Name}: {Method.Method} {Path}";
	}
	
	public static string?[] GetList(Assembly assembly)
	{
		var endPoint = assembly
			.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(Endpoint)) && !t.IsAbstract)
			.Select(t => (Endpoint?)Activator.CreateInstance(t))
			.Select(x => x?.ToString())
			.ToArray();
		return endPoint;
	}

	public static Endpoint? GetEndPoint(Assembly assembly, HttpListenerContext context)
	{
		var method = new HttpMethod(context.Request.HttpMethod);
		var path = context.Request.Url?.LocalPath ?? string.Empty;
		
		var endPoints = assembly
			.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(Endpoint)) && !t.IsAbstract)
			.Select(t => (Endpoint?)Activator.CreateInstance(t))
			.ToArray();

		foreach (var endpoint in endPoints)
		{
			if (endpoint?.Method == method && endpoint.Path == path)
				return endpoint;
		}

		if (endPoints.Length > 1)
			throw new Exception($"To many endpoints with same method and path, {string.Join("\n", endPoints.Select(x => x?.ToString()))}");

		return endPoints.FirstOrDefault();
	}
}