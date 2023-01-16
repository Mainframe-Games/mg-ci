using Deployment.Configs;
using Deployment.Misc;

namespace Deployment.Deployments;

public class ItchioDeploy
{
	private readonly string _location;
	private readonly string _username;
	private readonly string _gameName;

	private string ButlerExe => 
		Environment.OSVersion.Platform == PlatformID.Unix
		? $"{_location}/butler"
		: $"{_location}/butler.exe";

	public ItchioDeploy(ItchioConfig config)
	{
		_location = config.Location;
		_username = config.Username;
		_gameName = config.Game;
	}

	public void Deploy(IEnumerable<string> dirs, string version)
	{
		if (string.IsNullOrEmpty(version))
			throw new NullReferenceException("itchio version param is null");
		
		// butler push mygame user/mygame:win32-final --userversion 1.1.0
		foreach (var dir in dirs)
		{
			var target = dir.Split("/")[^1].Replace("_demo", "");
			Cmd.Run(ButlerExe, $"push \"{dir}\" {_username}/{_gameName}:{target} --userversion {version}");
		}
	}
}