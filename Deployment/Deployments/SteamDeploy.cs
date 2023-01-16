using Deployment.Configs;
using Deployment.Misc;
using Deployment.Server;

namespace Deployment.Deployments;

public class SteamDeploy
{
	private readonly SteamConfig _config;
	private readonly string? _steamPath;
	
	private string ContentDir => $"{_steamPath}/content";
	private string OutputDir => $"{_steamPath}/output";
	private string SteamCmdExe =>
		Environment.OSVersion.Platform == PlatformID.Unix
		? $"{_steamPath}/builder_osx/steamcmd.sh"
		: $"{_steamPath}/builder/steamcmd.exe";
	
	public SteamDeploy(SteamConfig config, string? steamPath)
	{
		_config = config;
		_steamPath = steamPath;
	}

	public void Deploy(IReadOnlyList<string> dirs, string description)
	{
		if (dirs.Count == 0)
			throw new Exception("No directories given for steam upload");
		
		foreach (var dir in dirs)
		{
			var dest = $"{ContentDir}/{dir.Split("/")[^1]}";
			CopyFilesRecursively(dir, dest);
		}

		var vdfPath = Path.Combine(Environment.CurrentDirectory, _config.VdfPath);
		var setLive = _config.SetLive;

		SetVdfProperties(vdfPath, 
			("Desc", description),
			("SetLive", setLive),
			("ContentRoot", ContentDir),
			("BuildOutput", OutputDir));

		var username = ServerConfig.Instance.Steam.Username;
		var password = ServerConfig.Instance.Steam.Password;
		var args = $"+login {username} {password} +run_app_build \"{vdfPath}\" +quit";
		Cmd.Run(SteamCmdExe, args);
	}

	private static string? GetInput(string write)
	{
		Console.Write(write);
		return Console.ReadLine();
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

	public void ClearContentFolder()
	{
		if (Directory.Exists(ContentDir))
			Directory.Delete(ContentDir, true);
		
		Directory.CreateDirectory(ContentDir);
	}

	/// <summary>
	/// Src: https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
	/// </summary>
	/// <param name="sourcePath"></param>
	/// <param name="targetPath"></param>
	private static void CopyFilesRecursively(string sourcePath, string targetPath)
	{
		// Now Create all of the directories
		foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
			Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

		// Copy all the files & Replaces any files with the same name
		foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
			File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
	}
}