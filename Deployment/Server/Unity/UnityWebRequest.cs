using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Server.Unity;

public abstract class UnityWebRequest
{
	protected string? AuthToken { get; }

	protected UnityWebRequest(string keyId, string secretKey)
	{
		AuthToken = $"Basic {Base64Key.Generate(keyId, secretKey)}";
	}

	/// <summary>
	/// Docs: https://services.docs.unity.com/auth/v1/#tag/Authentication/operation/exchangeToStateless
	/// </summary>
	/// <param name="projectId"></param>
	/// <param name="keyId"></param>
	/// <param name="secretKey"></param>
	/// <returns></returns>
	public static async Task<string> GetStatelessAccessTokenAsync(string projectId, string keyId, string secretKey)
	{
		var url = $"https://services.api.unity.com/auth/v1/token-exchange?projectId={projectId}";
		var token = $"Basic {Base64Key.Generate(keyId, secretKey)}";
		var res = await Web.SendAsync(HttpMethod.Post, url, $"Basic: {token}");
		var accessToken = JObject.Parse(res.Content).SelectToken("accessToken", true)?.ToString();
		return accessToken ?? string.Empty;
	}
}