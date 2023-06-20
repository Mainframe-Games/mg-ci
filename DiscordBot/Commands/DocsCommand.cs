using System.Diagnostics;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public class DocsCommand : Command
{
	private const string DOCS_URL = "https://github.com/Mainframe-Games/UnityDeployment/blob/master/DiscordBot/README.md#slash-commands";
	public override string? CommandName => "docs";
	public override string? Description => "Takes you to docs page";
	
	public static void OpenUrl()
	{
		if (OperatingSystem.IsMacOS())
			Process.Start("open", DOCS_URL);
		else
			Process.Start(new ProcessStartInfo(DOCS_URL) { UseShellExecute = true });
	}

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		Process.Start(DOCS_URL);
		await command.RespondAsync($"Opened {DOCS_URL}");
	}
}