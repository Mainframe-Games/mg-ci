using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public class ServerCommand : Command
{
	public ServerCommand(string? commandName, string? description) : base(commandName, description)
	{
	}

	public override async Task ExecuteAsync(SocketSlashCommand command, IUser user)
	{
		await NotImplemented(command, user);
	}
}