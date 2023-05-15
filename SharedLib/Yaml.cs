using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;

namespace SharedLib;

/// <summary>
/// YAML helper
/// </summary>
public class Yaml
{
	protected readonly string? _path;
	protected readonly string[] _lines;
	private readonly JObject _jObject;

	public Yaml(string? path, int skip = 3)
	{
		var file = new FileInfo(path);
		if (!file.Exists)
			throw new FileNotFoundException(path);

		_path = path;
		_lines = File.ReadAllLines(path);
		_jObject = YamlToJson(path, skip);
	}

	private static JObject YamlToJson(string path, int skipLines = 3)
	{
		var ymlLines = File.ReadAllLines(path);
		var yml = string.Join("\n", ymlLines.Skip(skipLines));

		// convert string/file to YAML object
		var r = new StringReader(yml);
		var deserializer = new Deserializer();
		var yamlObject = deserializer.Deserialize(r);

		var serializer = new Newtonsoft.Json.JsonSerializer();
		var writer = new StringWriter();
		serializer.Serialize(writer, yamlObject);
		var json = writer.ToString();
		var jObj = JObject.Parse(json);
		return jObj;
	}

	public T GetValue<T>(string path)
	{
		var token = _jObject.SelectToken(path, true);
		return token.Value<T>() ?? default(T);
	}

	public string? GetProjPropertyValue(params string[] propertyNames)
	{
		var index = GetProjPropertyLineIndex(propertyNames);
		return _lines[index].Replace($"{propertyNames[^1]}:", string.Empty).Trim();
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
}