using Deployment.Server.Config;
using SharedLib;

namespace Deployment.Server.Unity;

public abstract class UnityWebRequest
{
	protected UnityServicesConfig? Config { get; }
	protected string? AuthToken { get; }
	
	public UnityWebRequest()
	{
		Config = ServerConfig.Instance.UnityServices;
		AuthToken = $"Basic {Base64Key.Generate(Config.AccessKey, Config.SecretKey)}";
	}
}