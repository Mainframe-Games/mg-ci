using Deployment.Server;
using SharedLib;

namespace Deployment.Deployments;

public class SteamDeploy
{
	private SteamServerConfig Config { get; }
	private string VdfPath { get; }

	public SteamDeploy(string? vdfPath, SteamServerConfig? steamConfig)
	{
		VdfPath = vdfPath ?? string.Empty;
		Config = steamConfig ?? new SteamServerConfig();
	}

	public void Deploy(string description)
	{
		var vdfPath = Path.Combine(Environment.CurrentDirectory, VdfPath);
		SetVdfProperties(vdfPath, ("Desc", description));

		var path = Config.Path ?? string.Empty;
		var username = Config.Username;
		var password = Config.Password;
		var args = $"+login {username} {password} +run_app_build \"{vdfPath}\" +quit";
		Cmd.Run(path, args);
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