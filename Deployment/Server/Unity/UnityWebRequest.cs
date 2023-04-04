using SharedLib;

namespace Deployment.Server.Unity;

public abstract class UnityWebRequest
{
	protected string? AuthToken { get; }
	
	public UnityWebRequest(string accessKey, string secretKey)
	{
		AuthToken = $"Basic {Base64Key.Generate(accessKey, secretKey)}";
	}
}