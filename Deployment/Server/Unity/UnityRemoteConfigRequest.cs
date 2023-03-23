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
			if (prop["key"]?.ToString() != key)
				continue;
			
			var type = prop["type"]?.ToString();
			var convertedValue = ConvertType(type, value);
			prop["value"] = convertedValue;
		}

		await Web.SendAsync(HttpMethod.Put, url, AuthToken, body);
		Logger.Log($"Remote Config key: {key} updated to value: {value}");
	}

	private static JValue ConvertType(string? type, object? value)
	{
		return type switch
		{
			"string" => new JValue(Convert.ToString(value)),
			"int" => new JValue(Convert.ToInt32(value)),
			"float" => new JValue(Convert.ToSingle(value)),
			"bool" => new JValue(Convert.ToBoolean(value)),
			_ => throw new ArgumentException($"Type not supported '{type}'")
		};
	}
}