using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public class DocsCommand : Command
{
	private const string DOCS_URL = "https://github.com/Mainframe-Games/UnityDeployment/blob/master/DiscordBot/README.md#slash-commands";
	public override string? CommandName => "docs";
	public override string? Description => "Takes you to docs page";
	
	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		await Task.CompletedTask;
		return new CommandResponse("Docs", DOCS_URL);
	}
}