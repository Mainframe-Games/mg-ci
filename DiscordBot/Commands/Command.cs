using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public abstract class Command
{
	public string? CommandName { get; }
	public string? Description { get; }

	protected Command(string? commandName, string? description)
	{
		CommandName = commandName;
		Description = description;
	}

	public virtual SlashCommandProperties Build()
	{
		return CreateCommand().Build();
	}
	
	public abstract Task ExecuteAsync(SocketSlashCommand command, IUser user);
	
	protected SlashCommandBuilder CreateCommand()
	{
		return new SlashCommandBuilder()
			.WithName(CommandName)
			.WithDescription(Description);
	}

	protected static async Task NotImplemented(SocketSlashCommand command, IUser user)
	{
		await command.RespondError(user, "Not Implemented", "Command not implemented yet");
	}
}