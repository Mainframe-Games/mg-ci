using System.Net;
using System.Reflection;
using Deployment.Server;
using Server.RemoteBuild;

namespace Server;

public class ServerInfo : IRemoteControllable
{
	public string? Version { get; set; }
	public string? StartTime { get; set; }
	public string? RunTime { get; set; }

	public ServerInfo(ListenServer listenServer)
	{
		Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
		StartTime = listenServer.ServerStartTime.ToString("u");
		RunTime = (DateTime.Now - listenServer.ServerStartTime).ToString();
	}
		
	public ServerResponse Process()
	{
		return new ServerResponse(HttpStatusCode.OK, this);
	}
}