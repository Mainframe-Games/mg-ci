namespace SharedLib;

/// <summary>
/// YAML helper
/// TODO: Get an actual YAML parser for C#
/// </summary>
public class Yaml
{
	protected readonly string? _path;
	protected readonly string[] _lines;

	public Yaml(string? path)
	{
		var file = new FileInfo(path);
		if (!file.Exists)
			throw new FileNotFoundException(path);
		
		_path = path;
		_lines = File.ReadAllLines(path);
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
			if (index == propertyNames.Length - 1 && _lines[i].Contains(propName))
				return i;

			// increase index when prop found
			if (line.Contains(propName))
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