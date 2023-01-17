namespace Deployment.Configs;

public class TargetConfig
{
	public UnityTarget? Target { get; set; }
	public string? Settings { get; set; }
	public string? UnityPath { get; set; }
	public string? ExecuteMethod { get; set; }
	public string? BuildPath { get; set; }
	
	public string GetUnityPath(string unityVersion)
	{
		var path = !string.IsNullOrEmpty(UnityPath) 
			? UnityPath
			: BuildContainer.GetDefaultUnityPath();

		var exePath = path.Replace("{unityVersion}", unityVersion);
		return exePath;
	}

	public string GetExecuteMethod()
	{
		return !string.IsNullOrEmpty(ExecuteMethod) 
			? ExecuteMethod
			: BuildContainer.GetDefaultExecuteMethod();
	}
}