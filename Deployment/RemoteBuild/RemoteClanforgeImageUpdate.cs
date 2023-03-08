using Deployment.Deployments;
using Deployment.Server.Config;

namespace Deployment.RemoteBuild;

public class RemoteClanforgeImageUpdate : IRemoteControllable
{
	public ClanforgeConfig? Config { get; set; }
	public string? Desc { get; set; }
	
	public async Task<string> ProcessAsync()
	{
		var clanforge = new ClanForgeDeploy(Config, Desc);
		await clanforge.Deploy();
		return "ok";
	}
}