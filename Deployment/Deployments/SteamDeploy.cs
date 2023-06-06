using System.Text;
using SharedLib;

namespace Deployment.Deployments;

public class SteamDeploy
{
	private readonly string? _path; // steamcmd path
	private readonly string? _username;
	private readonly string? _password;
	private readonly string? _vdfPath;

	public SteamDeploy(string? vdfPath, string? password, string? username, string? path)
	{
		_vdfPath = vdfPath;
		_password = password;
		_username = username;
		_path = path;
	}

	public void Deploy(string description, string setLive)
	{
		var vdfPath = Path.Combine(Environment.CurrentDirectory, _vdfPath);
		SetVdfProperties(vdfPath, ("Desc", description), ("SetLive", setLive));

		var args = new StringBuilder();
		args.Append("+login");
		args.Append($" {_username}");
		args.Append($" {_password}");
		args.Append($" +run_app_build \"{vdfPath}\"");
		args.Append(" +quit");
		
		var (code, output) = Cmd.Run(_path, args.ToString(), false);
		
		if (output.Contains("FAILED", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Steam upload failed ({code}): {output}");
	}

	private static void SetVdfProperties(string vdfPath, params (string key, string value)[] values)
	{
		if (!File.Exists(vdfPath))
			throw new FileNotFoundException($"File doesn't exist at {vdfPath}");
		
		var vdfLines = File.ReadAllLines(vdfPath);

		foreach ((string key, string value) in values)
		{
			if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
				continue;
			
			foreach (var line in vdfLines)
			{
				if (!line.Contains($"\"{key}\""))
					continue;

				var index = Array.IndexOf(vdfLines, line);
				vdfLines[index] = $"\t\"{key}\" \"{value}\"";
			}
		}

		File.WriteAllText(vdfPath, string.Join("\n", vdfLines));
	}
}