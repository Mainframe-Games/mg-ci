using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SharedLib;

/// <summary>
/// Wrapper class for JsonConvert
/// </summary>
public static class Json
{
	private static readonly JsonSerializerSettings _settings = new()
	{
		ContractResolver = new CamelCasePropertyNamesContractResolver(),
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Ignore,
		Converters =
		{
			new StringEnumConverter(),
		}
	};
	
	public static string Serialise(object? obj)
	{
		return JsonConvert.SerializeObject(obj, _settings);
	}
	
	public static T? Deserialise<T>(string jsonStr)
	{
		return JsonConvert.DeserializeObject<T>(jsonStr, _settings);
	}
}