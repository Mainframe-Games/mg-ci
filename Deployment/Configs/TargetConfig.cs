namespace Deployment.Configs;

public class TargetConfig
{
	public UnityTarget? Target { get; set; }
	public string? Settings { get; set; }
	public string? ExecuteMethod { get; set; }
	public string? BuildPath { get; set; }
	public string? VersionExtension { get; set; }
}