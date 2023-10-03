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
	public abstract string Path { get; }
	
	public abstract Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext);

	public override string ToString()
	{
		return $"{GetType().Name}: {Method.Method} {Path}";
	}

	public static Endpoint? GetEndPoint(HttpListenerContext context)
	{
		var method = new HttpMethod(context.Request.HttpMethod);
		var path = context.Request.Url?.LocalPath ?? string.Empty;
		
		var endPoint = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(Endpoint)) && !t.IsAbstract)
			.Select(t => (Endpoint?)Activator.CreateInstance(t))
			.Where(x => x?.Method == method && x.Path == path)
			.ToArray();

		if (endPoint.Length > 1)
			throw new Exception($"To many endpoints with same method and path, {string.Join("\n", endPoint.Select(x => x?.ToString()))}");

		return endPoint.FirstOrDefault();
	}
}