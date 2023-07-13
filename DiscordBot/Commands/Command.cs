using Discord;
using Discord.WebSocket;
using DiscordConfig = DiscordBot.Configs.DiscordConfig;

namespace DiscordBot.Commands;

public abstract class Command
{
	public abstract string? CommandName { get; }
	public abstract string? Description { get; }

	public virtual SlashCommandProperties Build()
	{
		return CreateCommand().Build();
	}
	
	public abstract Task<CommandResponse> ExecuteAsync(SocketSlashCommand command);
	
	public virtual async Task ModifyOptions(SocketSlashCommand socketSlashCommand, SocketInteraction socketInteraction)
	{
		await Task.CompletedTask;
	}
	
	protected SlashCommandBuilder CreateCommand()
	{
		return new SlashCommandBuilder()
			.WithName(CommandName)
			.WithDescription(Description);
	}

	protected static string? GetOptionValueString(SocketSlashCommand command, string optionName)
	{
		foreach (var option in command.Data.Options)
		{
			if (option.Name == optionName)
				return option.Value?.ToString();
		}
		return null;
	}
	
	protected static int GetOptionValueInt(SocketSlashCommand command, string optionName)
	{
		foreach (var option in command.Data.Options)
		{
			if (option.Name == optionName)
				return (int)(long)option.Value;
		}
		return -1;
	}
	

	protected static async Task NotImplemented(SocketSlashCommand command)
	{
		await command.RespondError(command.User, "Not Implemented", "Command not implemented yet");
	}
}