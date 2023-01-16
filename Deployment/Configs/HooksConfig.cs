namespace Deployment.Configs;

public class HooksConfig
{
	public HookWrap Slack { get; set; }
	public HookWrap Discord { get; set; }
}

public struct HookWrap
{
	public string Url { get; set; }
	public string Title { get; set; }
}