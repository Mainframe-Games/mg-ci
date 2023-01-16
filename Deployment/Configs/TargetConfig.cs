namespace Deployment.Configs;

public class TargetConfig
{
	public string? Target { get; set; }
	public string? Settings { get; set; }
	public string? UnityPath { get; set; }
	public string? ExecuteMethod { get; set; }
	public string? BuildPath { get; set; }
	public bool IgnoreSteamUpload { get; set; }
	public string? OS { get; set; }
	public string? OffloadUrl { get; set; }
	
	public bool TryGetUnityPath(string unityVersion, out string exePath)
	{
		exePath = UnityPath.Replace("{unityVersion}", unityVersion);

		if (File.Exists(exePath))
			return true;
		
		// if incorrect os throw exception, else ignore
		if (string.IsNullOrEmpty(OS) || OperatingSystem.IsOSPlatform(OS))
			throw new DirectoryNotFoundException(exePath);

		return false;
	}
}