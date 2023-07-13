using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot.Commands;

public class BuildCommand : Command
{
	public override string? CommandName => DiscordWrapper.Config.CommandName ?? "start-build";
	public override string? Description => "Starts a build from discord";

	public override SlashCommandProperties Build()
	{
		return CreateCommand()
			.AddOptions(CommandUtils.WorkspaceOptions())
			.AddOptions(BuildArgumentsOptions())
			.Build();
	}

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
			var workspaceName = GetOptionValueString(command, "workspace");
			var args = GetOptionValueString(command, "args");
			await command.DeferAsync();
			
			// request to build server
			var body = new JObject
			{
				["workspaceBuildRequest"] = new JObject
				{
					["workspaceName"] = workspaceName,
					["args"] = args,
				}
			};
			var res = await Web.SendAsync(HttpMethod.Post, DiscordWrapper.Config.BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(command.User, "Build Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await command.RespondErrorDelayed(command.User, "Build Server request failed", e.Message);
		}
	}

	private static SlashCommandOptionBuilder BuildArgumentsOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("args")
			.WithDescription("Arguments send to build server")
			.WithType(ApplicationCommandOptionType.String);
		return opt;
	}
}