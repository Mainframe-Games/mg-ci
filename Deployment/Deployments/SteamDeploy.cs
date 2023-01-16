using Deployment.Configs;
using Deployment.Misc;
using Deployment.Server;

namespace Deployment.Deployments;

public class SteamDeploy
{
	private readonly SteamConfig _config;
	private readonly string? _steamPath;
	
	private string SteamCmdExe =>
		Environment.OSVersion.Platform == PlatformID.Unix
		? $"{_steamPath}/builder_osx/steamcmd.sh"
		: $"{_steamPath}/builder/steamcmd.exe";
	
	public SteamDeploy(SteamConfig config, string? steamPath)
	{
		_config = config;
		_steamPath = steamPath;
	}

	public void Deploy(string description)
	{
		var vdfPath = Path.Combine(Environment.CurrentDirectory, _config.VdfPath);
		var contentDir = Path.Combine(Environment.CurrentDirectory, "Builds");
		var outputDir = Path.Combine(Environment.CurrentDirectory, "Builds", "SteamOutput");
		var setLive = _config.SetLive;

		SetVdfProperties(vdfPath, 
			("Desc", description),
			("SetLive", setLive),
			("ContentRoot", contentDir),
			("BuildOutput", outputDir));

		var username = ServerConfig.Instance.Steam.Username;
		var password = ServerConfig.Instance.Steam.Password;
		var args = $"+login {username} {password} +run_app_build \"{vdfPath}\" +quit";
		Cmd.Run(SteamCmdExe, args);
	}

	private static void SetVdfProperties(string vdfPath, params (string key, string value)[] values)
	{
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

		File.WriteAllLines(vdfPath, vdfLines);
	}
}