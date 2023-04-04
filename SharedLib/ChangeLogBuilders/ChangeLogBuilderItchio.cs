namespace SharedLib.ChangeLogBuilders;

public class ChangeLogBuilderItchio : ChangelogBuilder
{
	protected override Markup List => new("<ul>", "</ul>");
	protected override Markup ListItem => new("<li>", "</li>");
	protected override Markup Bold => new("<strong>", "</strong>");
}