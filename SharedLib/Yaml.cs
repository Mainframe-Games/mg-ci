using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SharedLib;

/// <summary>
/// YAML helper
/// </summary>
public abstract class Yaml
{
	private readonly string? _path;
	protected readonly string[] _lines;
	private readonly JObject _jObject;

	protected Yaml(string? path, int skip = 3)
	{
		var file = new FileInfo(path);
		if (!file.Exists)
			throw new FileNotFoundException(path);

		_path = path;
		_lines = File.ReadAllLines(path);
		
		var yml = string.Join("\n", _lines.Skip(skip));
		_jObject = YamlToJson(yml);
	}

	private static JObject YamlToJson(string yml)
	{
		// convert string/file to YAML object
		var r = new StringReader(yml);
		var deserializer = new Deserializer();
		var yamlObject = deserializer.Deserialize(r);

		var serializer = new JsonSerializer();
		var writer = new StringWriter();
		serializer.Serialize(writer, yamlObject);
		var json = writer.ToString();
		var jObj = JObject.Parse(json);
		return jObj;
	}
	
	private static string JsonToYaml(JToken jsonObject)
	{
		var obj = jsonObject.ToObject<ExpandoObject>();
		var serializer = new Serializer();
		using var writer = new StringWriter();
		serializer.Serialize(writer, obj);
		var yaml = writer.ToString();
		return yaml;
	}

	public override string ToString()
	{
		return _jObject?.ToString() ?? string.Empty;
	}

	protected void SaveToFile()
	{
		File.WriteAllText(_path, string.Join("\n", _lines));
	}

	public virtual T GetValue<T>(string path)
	{
		try
		{
			var token = _jObject.SelectToken(path, true);
			return token.Value<T>() ?? default(T);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to find path `{path}`. {e.Message}");
			return default;
		}
	}
	
	public virtual T? GetObject<T>(string path) where T : class
	{
		try
		{
			var token = _jObject.SelectToken(path, true);
			return token.ToObject<T?>() ?? default(T?);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to find path `{path}`. {e.Message}");
			return default;
		}
	}

	public int GetProjPropertyLineIndex(params string[] propertyNames)
	{
		var index = 0;

		for (var i = 0; i < _lines.Length; i++)
		{
			var line = _lines[i];
			var propName = $"{propertyNames[index]}:";

			// last index, return value
			if (index == propertyNames.Length - 1 && _lines[i].Contains(propName, StringComparison.OrdinalIgnoreCase))
				return i;

			// increase index when prop found
			if (line.Contains(propName, StringComparison.OrdinalIgnoreCase))
				index++;
		}

		throw new KeyNotFoundException($"Failed to find path: '{string.Join(", ", propertyNames)}'");
	}

	protected static string ReplaceText(string? line, string? newValue)
	{
		if (line == null)
			throw new ArgumentNullException($"{nameof(line)} params is null");

		var oldValue = line.Split(":").Last().Trim();

		if (string.IsNullOrEmpty(oldValue))
			throw new NullReferenceException($"{nameof(oldValue)} is null on line '{line}'");

		var replacement = line.Replace(oldValue, newValue);
		return replacement;
	}
	
	public static T Deserialise<T>(string? path, int skip = 3)
	{
		var file = new FileInfo(path);
		
		if (!file.Exists)
			throw new FileNotFoundException(path);

		var lines = File.ReadAllLines(path);
		var yml = string.Join("\n", lines.Skip(skip));
		
		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(new PascalCaseNamingConvention())
			.IgnoreUnmatchedProperties()
			.Build();
		
		var obj = deserializer.Deserialize<T>(yml);
		return obj;
	}
}