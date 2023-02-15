using Deployment.Server;
using SharedLib;

namespace Deployment.Deployments;

public class SteamDeploy
{
	private readonly string? _vdfPath;
	private readonly string? _steamPath;

	public SteamDeploy(string? vdfPath)
	{
		_vdfPath = vdfPath;
		_steamPath = ServerConfig.Instance.Steam.Path;
	}

	public void Deploy(string description)
	{
		var vdfPath = Path.Combine(Environment.CurrentDirectory, _vdfPath);
		var contentDir = Path.Combine(Environment.CurrentDirectory, "Builds");
		var outputDir = Path.Combine(Environment.CurrentDirectory, "Builds", "SteamOutput");

		SetVdfProperties(vdfPath, 
			("Desc", description)//,
			//("ContentRoot", contentDir),
			//("BuildOutput", outputDir)
			);

		var username = ServerConfig.Instance.Steam.Username;
		var password = ServerConfig.Instance.Steam.Password;
		var args = $"+login {username} {password} +run_app_build \"{vdfPath}\" +quit";
		Cmd.Run(_steamPath, args);
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