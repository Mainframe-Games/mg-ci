using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot.Commands;

public class CancelCommand : Command
{
	public override string? CommandName => "cancel-build";
	public override string? Description => "Will cancel a build";

	public override SlashCommandProperties Build()
	{
		return CreateCommand()
			.AddOptions(BuildOptionNumber("pipeline-id", "The pipelineId you want to cancel", false))
			.Build();
	}

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		var pipelineId = GetOptionValueNumber(command, "pipelineId");
		var body = new JObject { ["pipelineId"] = pipelineId };
		var res = await Web.SendAsync(HttpMethod.Post, $"{DiscordWrapper.Config.BuildServerUrl}/cancel", body: body);
		return new CommandResponse($"Cancelling pipelineId: {pipelineId}", res.Content);
	}
}