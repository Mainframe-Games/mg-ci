using System.CommandLine;

namespace MG_CLI;

/// <summary>
/// The GodotProjectSettings class provides functionality to manage project settings
/// for a Godot project. It allows users to retrieve or update specific settings
/// within the Godot project configuration file.
/// </summary>
/// <remarks>
/// This class is primarily used as part of the command-line interface (CLI) to
/// interact with Godot project settings. It provides commands for getting or setting
/// key-value pairs within the project settings file, enabling streamlined automation
/// for project configuration tasks.
/// </remarks>
/// <example>
/// The class supports the following commands:
/// - "set": Sets a specified project setting.
/// - "get": Retrieves the value of a specified project setting.
/// These commands utilize the CLI to interact with the configuration file.
/// </example>
public class GodotProjectSettings : Command
{
	private readonly Option<string> _set = new("--set", "-s")
	{
		HelpName = "Set a project setting"
	};
	
	private readonly Option<string> _get = new("--get", "-g")
	{
		HelpName = "Get a project setting"
	};
	
	public GodotProjectSettings() : base("settings", "Manage project settings")
	{
		Add(_set);
		Add(_get);
        SetAction(Run);
	}

	private async Task Run(ParseResult result, CancellationToken token)
	{
		var set = result.GetValue(_set);
		var get = result.GetValue(_get);
		
		var path = Path.GetFullPath(Environment.CurrentDirectory);
		var file = GodotUtils.GetProjectSettingsFile(path);
		var lines = await File.ReadAllLinesAsync(file.FullName, token);

		if (set is not null)
		{
			SetProjectSetting(set, lines);
			await FileEx.WriteAllLinesAsync(file.FullName, lines);
		}
		
		if (get is not null)
			GetProjectSetting(get, lines);
	}

	/// <summary>
	/// Updates the value of a specific project setting in the provided settings file lines.
	/// </summary>
	/// <param name="set">
	/// The setting to be updated in the format "key=value".
	/// The key is the setting name, and the value is the new value to be assigned.
	/// </param>
	/// <param name="lines">
	/// An array of strings representing the lines of the project settings file
	/// where the setting will be updated.
	/// </param>
	private static void SetProjectSetting(in string set, in string[] lines)
	{
		var setKey = set.Split('=')[0];
		var setValue = set.Split('=')[^1];
		
		for (var i = 0; i < lines.Length; i++)
		{
			var key = lines[i].Split("=")[0].Trim('"');
            
			if (key != setKey)
				continue;
			
			Log.Print($"Set: key={setValue}");
			lines[i] = $"{key}={setValue}";
		}
	}

	/// <summary>
	/// Retrieves the value of a specific project setting from the provided settings file lines
	/// and outputs it to the console. If the key is not found, an error message is displayed.
	/// </summary>
	/// <param name="getKey">
	/// The key of the project setting to retrieve. This represents the name of the setting whose value is being requested.
	/// </param>
	/// <param name="lines">
	/// An array of strings representing the lines of the project settings file from which the setting will be searched.
	/// </param>
	private static void GetProjectSetting(in string? getKey, in string[] lines)
	{
		foreach (var line in lines)
		{
			var key = line.Split("=")[0].Trim('"');
			var value = line.Split("=")[^1].Trim('"');
            
			if (key != getKey)
				continue;
			
			Console.WriteLine(value);
			return;
		}
		
		Log.PrintError($"No project setting found with key: {getKey}");
	}
}