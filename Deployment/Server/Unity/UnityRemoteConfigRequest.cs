using Newtonsoft.Json.Linq;
using SharedLib;

namespace Deployment.Server.Unity;

public class UnityRemoteConfigRequest : UnityWebRequest
{
	private string? ConfigId { get; }
	
	public UnityRemoteConfigRequest(string? configId)
	{
		ConfigId = configId;
	}

	public async Task UpdateConfig(string? key, object? value)
	{
		var url = Config?.BuildUrl("remote-config", $"configs/{ConfigId}");
		var currentConfig = await Web.SendAsync(HttpMethod.Get, url, AuthToken);
		var json = JObject.Parse(currentConfig.Content);

		var body = new JObject
		{
			["type"] = "settings",
			["value"] = json["value"]
		};

		foreach (var prop in (JArray)body["value"])
		{
			if (prop["key"]?.ToString() == key)
				prop["value"] = new JValue(value);
		}

		await Web.SendAsync(HttpMethod.Put, url, AuthToken, body);
		Logger.Log($"Remote Config key: {key} updated to value: {value}");
	}
}