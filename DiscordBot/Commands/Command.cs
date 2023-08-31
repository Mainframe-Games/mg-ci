using Discord;
using Discord.WebSocket;
using SharedLib;

namespace DiscordBot.Commands;

public abstract class Command
{
	protected static SlashCommandOptionBuilder WorkspaceOptions { get; private set; }
	
	public abstract string? CommandName { get; }
	public abstract string? Description { get; }

	public virtual SlashCommandProperties Build()
	{
		return CreateCommand().Build();
	}
	
	public abstract Task<CommandResponse> ExecuteAsync(SocketSlashCommand command);
	
	public virtual async Task ModifyOptions(SocketSlashCommand slashCommand)
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
	
	protected static long GetOptionValueNumber(SocketSlashCommand command, string optionName, long defaultNum = -1)
	{
		foreach (var option in command.Data.Options)
		{
			if (option.Name == optionName)
				return (long)option.Value;
		}
		return defaultNum;
	}
	
	protected static async Task NotImplemented(SocketSlashCommand command)
	{
		await command.RespondError("Not Implemented", "Command not implemented yet");
	}
	
	protected static SlashCommandOptionBuilder BuildOptionString(string name, string desc, bool required)
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithRequired(required)
			.WithDescription(desc)
			.WithType(ApplicationCommandOptionType.String);
		return opt;
	}
	
	public static SlashCommandOptionBuilder BuildOptionStringWithChoices(
		string name, string desc, bool required, IEnumerable<string> choices)
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithDescription(desc)
			.WithRequired(required)
			.WithType(ApplicationCommandOptionType.String);
		
		foreach (var choice in choices)
			opt.AddChoice(choice, choice);

		return opt;
	}

	protected static SlashCommandOptionBuilder BuildOptionNumber(string name, string desc, bool required)
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithRequired(required)
			.WithDescription(desc)
			.WithType(ApplicationCommandOptionType.Integer);
		return opt;
	}
	
	public static async Task IntialiseWorksapcesAsync()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("workspace")
			.WithDescription("List of available workspaces")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String);

		var workspaces = await DiscordWrapper.Config.SetWorkspaceNamesAsync();

		foreach (var workspace in workspaces)
		{
			Logger.Log($"Workspace: {workspace.Name}");
			opt.AddChoice(workspace.Name, workspace.Name);
		}

		WorkspaceOptions = opt;
	}
}