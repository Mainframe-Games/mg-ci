using Deployment.Server.Configs;

namespace Deployment.Server.Config;

public class UnityServicesConfig
{
	public CcdConfigServer Ccd { get; set; }
	public MultiplayConfigServer Multiplay { get; set; }
	public string? ProjectId { get; set; }
	public string? EnvironmentId { get; set; }
}