using System.Net;
using System.Reflection;
using SharedLib.Server;

namespace Server.Endpoints;

public class ServerInfo : Endpoint<object>
{
	public override string Path => "/info";
	// protected override bool IgnoreBodyProcess => true;


	protected override async Task<ServerResponse> GET()
	{
		await Task.CompletedTask;
		
		var runTime = DateTime.Now - Server.ServerStartTime;

		var packet = new Dictionary<string, object>
		{
			["Version"] = App.Version,
			["StartTime"] = Server.ServerStartTime.ToString("u"),
			["RunTime"] = $"{runTime.Days}d {runTime.Hours}h {runTime.Minutes}m {runTime.Seconds}s",
			["EndPoints"] = EndPointUtils.GetList(Assembly.GetExecutingAssembly()),
		};
		
		return new ServerResponse(HttpStatusCode.OK, packet);
	}
}