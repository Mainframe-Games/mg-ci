using System.Net;
using System.Reflection;

namespace SharedLib.Server;

public static class EndPointUtils
{
	private static IEndpoint?[] GetAll(Assembly assembly)
	{
		return assembly
			.GetTypes()
			.Where(t => typeof(IEndpoint).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
			.Select(t => (IEndpoint?)Activator.CreateInstance(t))
			.ToArray();
	}
	
	public static string?[] GetList(Assembly assembly)
	{
		var endPoints = GetAll(assembly);
		var	endPoint = endPoints
			.Select(x => x?.ToString())
			.ToArray();
		return endPoint;
	}

	public static IEndpoint? GetEndPoint(Assembly assembly, HttpListenerContext context)
	{
		var path = context.Request.Url?.LocalPath ?? string.Empty;
		var endPoints = GetAll(assembly);
		foreach (var endpoint in endPoints)
		{
			if (endpoint?.Path == path)
				return endpoint;
		}

		if (endPoints.Length > 1)
			throw new Exception($"To many endpoints with same method and path, {string.Join("\n", endPoints.Select(x => x?.ToString()))}");

		return endPoints.FirstOrDefault();
	}
}