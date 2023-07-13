using Discord;
using Discord.WebSocket;
using SharedLib;

namespace DiscordBot.Commands;

public class CommitsCommand : Command
{
	public override string? CommandName => "commits";
	public override string? Description => "Gets a range of commits from server";

	public override SlashCommandProperties Build()
	{
		return CreateCommand()
			.AddOption(CommandUtils.WorkspaceOptions())
			.AddOption(BuildArgumentsOptions("csfrom", "Changeset to start from"))
			.AddOption(BuildArgumentsOptions("csto", "Changeset to go to"))
			.Build();
	}
	
	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		var workspace = GetOptionValueString(command, "workspace");
		var csfrom = GetOptionValueString(command, "csfrom");
		var csto = GetOptionValueString(command, "csto");
		
		var url = $"{DiscordWrapper.Config.BuildServerUrl}/commits?workspace={workspace}&csfrom={csfrom}&csto={csto}";
		var res = await Web.SendAsync(HttpMethod.Get, url);
		await command.RespondSuccessDelayed(command.User, $"Commits from {csfrom} to {csto}", res.Content);
		return new CommandResponse($"Commits from {csfrom} to {csto}", res.Content);
	}
	
	private static SlashCommandOptionBuilder BuildArgumentsOptions(string name, string desc)
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName(name)
			.WithRequired(true)
			.WithDescription(desc)
			.WithType(ApplicationCommandOptionType.Integer);
		return opt;
	}
}