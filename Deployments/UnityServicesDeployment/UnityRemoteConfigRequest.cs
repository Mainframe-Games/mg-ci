using Newtonsoft.Json.Linq;
using UnityServicesDeployment.Utils;

namespace UnityServicesDeployment;

public class UnityRemoteConfigRequest(string keyId, string secretKey, string configId)
    : UnityWebRequest(keyId, secretKey)
{
    private string? ConfigId { get; } = configId;

    public async Task UpdateConfig(string projectId, string? key, object? value)
    {
        var url =
            $"https://services.api.unity.com/remote-config/v1/projects/{projectId}/configs/{ConfigId}";
        var res = await Web.SendAsync(HttpMethod.Get, url, AuthToken, null);
        var json = JObject.Parse(res);

        var body = new JObject { ["type"] = "settings", ["value"] = json["value"] };

        foreach (var prop in (JArray)body["value"])
        {
            if (prop["key"]?.ToString() != key)
                continue;

            var type = prop["type"]?.ToString();
            var convertedValue = ConvertType(type, value);
            prop["value"] = convertedValue;
        }

        await Web.SendAsync(HttpMethod.Put, url, AuthToken, body);
        Console.WriteLine($"Remote Config key: {key} updated to value: {value}");
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
