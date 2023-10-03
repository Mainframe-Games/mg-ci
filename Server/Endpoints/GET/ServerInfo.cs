using System.Net;
using System.Reflection;
using SharedLib.Server;

namespace Server.Endpoints.GET;

public class ServerInfo : Endpoint
{
	public override HttpMethod Method => HttpMethod.Get;
	public override string Path => "/info";
	
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		await Task.CompletedTask;
		
		var runTime = DateTime.Now - server.ServerStartTime;

		var packet = new Dictionary<string, object>
		{
			["Version"] = App.Version,
			["StartTime"] = server.ServerStartTime.ToString("u"),
			["RunTime"] = $"{runTime.Days}d {runTime.Hours}h {runTime.Minutes}m {runTime.Seconds}s",
			["EndPoints"] = GetList(Assembly.GetExecutingAssembly()),
		};
		
		return new ServerResponse(HttpStatusCode.OK, packet);
	}
}