using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;
using SharedLib.BuildToDiscord;

namespace DiscordBot.Commands;

public class BuildCommand : Command
{
	public override string? CommandName => DiscordWrapper.Config.CommandName ?? "start-build";
	public override string? Description => "Starts a build from discord";

	public override SlashCommandProperties Build()
	{
		return CreateCommand()
			.AddOptions(WorkspaceOptions)
			.AddOptions(BuildOptionString("args", "Arguments send to build server", false))
			.Build();
	}

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
			var workspaceName = GetOptionValueString(command, "workspace");
			var args = GetOptionValueString(command, "args");
			
			// request to build server
			var body = new JObject
			{
				["workspaceBuildRequest"] = new JObject
				{
					["workspaceName"] = workspaceName,
					["args"] = args,
					["commandId"] = command.Id,
					["discordAddress"] = DiscordWrapper.Config.ListenServer?.Address
				}
			};

			var res = await Web.SendAsync(HttpMethod.Post, DiscordWrapper.Config.BuildServerUrl, body: body);
			var obj = Json.Deserialise<BuildPipelineResponse>(res.Content);
			return new CommandResponse("Build Started", obj?.ToString() ?? string.Empty);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return new CommandResponse("Build Server request failed", e.Message, true);
		}
	}
}