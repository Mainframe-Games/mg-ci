namespace SharedLib.ChangeLogBuilders;

public struct Markup
{
	public readonly string Start;
	public readonly string End;

	public Markup(string start, string end)
	{
		Start = start;
		End = end;
	}
}