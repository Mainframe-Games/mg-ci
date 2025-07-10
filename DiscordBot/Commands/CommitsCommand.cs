using System.Net.Http;
using System.Threading.Tasks;
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
			.AddOption(WorkspaceOptions)
			.AddOption(BuildOptionNumber("csfrom", "Changeset to start from", true))
			.AddOption(BuildOptionNumber("csto", "Changeset to go to", true))
			.Build();
	}
	
	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		var workspace = GetOptionValueString(command, "workspace");
		var csfrom = GetOptionValueString(command, "csfrom");
		var csto = GetOptionValueString(command, "csto");
		
		var url = $"{DiscordWrapper.Config.BuildServerUrl}/commits?workspace={workspace}&csfrom={csfrom}&csto={csto}";
		var res = await Web.SendAsync(HttpMethod.Get, url);
		return new CommandResponse($"Commits from {csfrom} to {csto}", res.Content);
	}
}