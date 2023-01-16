namespace Deployment.ChangeLogBuilders;

public class ChangeLogBuilderDiscord : ChangelogBuilder
{
	protected override Markup List => new("", "");
	protected override Markup ListItem => new("-", "");
	protected override Markup Bold => new("**", "**");
}