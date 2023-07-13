using System.Net;
using Deployment.Server;
using Server.RemoteBuild;
using SharedLib;

namespace Server;

public class ServerInfo : IRemoteControllable
{
	public string? Version { get; set; }
	public string? StartTime { get; set; }
	public string? RunTime { get; set; }

	public ServerInfo(ListenServer listenServer)
	{
		Version = AppVersion.VERSION;// Assembly.GetExecutingAssembly().GetName().Version?.ToString();
		StartTime = listenServer.ServerStartTime.ToString("u");

		var runTime = DateTime.Now - listenServer.ServerStartTime;
		RunTime = $"{runTime.Days}d {runTime.Hours}h {runTime.Minutes}m";
	}
		
	public ServerResponse Process()
	{
		return new ServerResponse(HttpStatusCode.OK, this);
	}
}