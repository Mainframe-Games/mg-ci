namespace Deployment.Configs;

/// <summary>
/// Build config local to each Unity project
/// </summary>
public sealed class BuildConfig
{
    public DeployContainerConfig? Deploy { get; }
    public HooksConfig[]? Hooks { get; }
}

public class DeployContainerConfig
{
    public string[]? Steam { get; set; }
    public bool? Clanforge { get; set; }
    public bool? AppleStore { get; set; }
    public bool? GoogleStore { get; set; }
    public bool? S3 { get; set; }
}

public class HooksConfig
{
    public string? Url { get; set; }
    public string? Title { get; set; }
    public bool? IsErrorChannel { get; set; }

    public bool IsDiscord() => Url?.StartsWith("https://discord.com/") ?? false;

    public bool IsSlack() => Url?.StartsWith("https://hooks.slack.com/") ?? false;
}
