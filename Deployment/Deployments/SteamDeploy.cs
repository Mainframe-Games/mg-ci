using SharedLib;

namespace Deployment.Deployments;

public class SteamDeploy
{
	private readonly string? _path; // steamcmd path
	private readonly string? _username;
	private readonly string? _password;
	private readonly string? _vdfPath;

	public bool IsCompleted { get; private set; }

	public SteamDeploy(string? vdfPath, string? password, string? username, string? path)
	{
		_vdfPath = vdfPath;
		_password = password;
		_username = username;
		_path = path;
	}

	public void Deploy(string description)
	{
		var vdfPath = Path.Combine(Environment.CurrentDirectory, _vdfPath);
		SetVdfProperties(vdfPath, ("Desc", description));
		var args = $"+login {_username} {_password} +run_app_build \"{vdfPath}\" +quit";
		Cmd.Run(_path, args);
		IsCompleted = true;
	}

	private static void SetVdfProperties(string vdfPath, params (string key, string value)[] values)
	{
		if (!File.Exists(vdfPath))
			throw new FileNotFoundException($"File doesn't exist at {vdfPath}");
		
		var vdfLines = File.ReadAllLines(vdfPath);

		foreach ((string key, string value) in values)
		{
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