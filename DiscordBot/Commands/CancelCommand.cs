using Discord.WebSocket;

namespace DiscordBot.Commands;

public class CancelCommand : Command
{
	public override string? CommandName => "cancel-build";
	public override string? Description => "Will cancel a build";
	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		await command.DeferAsync();
		await command.RespondErrorDelayed(command.User, "Not Implemented Yet", "This command isn't implemented yet");
	}
}