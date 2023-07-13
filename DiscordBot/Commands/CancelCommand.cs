using Discord.WebSocket;

namespace DiscordBot.Commands;

public class CancelCommand : Command
{
	public override string? CommandName => "cancel-build";
	public override string? Description => "Will cancel a build";
	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		await Task.CompletedTask;
		return new CommandResponse("Not Implemented Yet", "This command isn't implemented yet");
	}
}