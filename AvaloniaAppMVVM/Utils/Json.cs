using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AvaloniaAppMVVM.Utils;

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
		try
		{
			return JsonConvert.SerializeObject(obj, _settings);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to serialise object: {obj}");
			return string.Empty;
		}
	}

	public static T? Deserialise<T>(string jsonStr)
	{
		try
		{
			return JsonConvert.DeserializeObject<T>(jsonStr, _settings);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to deserialise to type: {typeof(T).Name}, json: {jsonStr}");
			throw;
		}
	}
}