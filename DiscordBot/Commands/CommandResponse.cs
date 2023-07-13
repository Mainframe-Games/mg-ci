namespace DiscordBot.Commands;

public readonly struct CommandResponse
{
	public readonly string Title;
	public readonly string Content;
	public readonly bool IsError;

	public CommandResponse(string title, string content, bool isError = false)
	{
		Title = title;
		Content = content;
		IsError = isError;
	}
}