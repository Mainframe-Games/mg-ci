using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public class TestCommand : Command
{
	public override string? CommandName => "test";
	public override string? Description => "Tests connection loop";
	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		await Task.CompletedTask;
		return new CommandResponse("Testing Discord Bot", "This is a test message");
	}
}