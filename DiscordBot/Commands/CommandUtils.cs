using Discord;

namespace DiscordBot.Commands;

public static class CommandUtils
{
	public static SlashCommandOptionBuilder WorkspaceOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("workspace")
			.WithDescription("List of available workspaces")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String);
		
		foreach (var workspace in DiscordWrapper.Config.Workspaces)
			opt.AddChoice(workspace, workspace);

		return opt;
	}
}