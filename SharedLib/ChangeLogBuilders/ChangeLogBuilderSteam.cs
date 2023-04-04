namespace SharedLib.ChangeLogBuilders;

public class ChangeLogBuilderSteam : ChangelogBuilder
{
	protected override Markup List => new("[list]", "[/list]");
	protected override Markup ListItem => new("[*]", "");
	protected override Markup Bold => new("[b]", "[/b]");
}