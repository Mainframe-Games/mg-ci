using System.Net;
using Newtonsoft.Json;
using SharedLib.Server;

namespace Server.Endpoints.GET;

public class ServerInfo : Endpoint
{
	public string? Version { get; set; }
	public string? StartTime { get; set; }
	public string? RunTime { get; set; }

	[JsonIgnore] public override HttpMethod Method => HttpMethod.Get;
	[JsonIgnore] public override string Path => "/info";
	
	public override async Task<ServerResponse> ProcessAsync(ListenServer server, HttpListenerContext httpContext)
	{
		await Task.CompletedTask;
		
		Version = App.Version;
		StartTime = server.ServerStartTime.ToString("u");

		var runTime = DateTime.Now - server.ServerStartTime;
		RunTime = $"{runTime.Days}d {runTime.Hours}h {runTime.Minutes}m";
		
		return new ServerResponse(HttpStatusCode.OK, this);
	}
}