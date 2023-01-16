namespace Deployment.Configs;

public class HooksConfig
{
	public string Url { get; set; }
	public string Title { get; set; }

	public bool IsDiscord() => Url.StartsWith("https://discord.com/");
	public bool IsSlack() => Url.StartsWith("https://hooks.slack.com/");
}