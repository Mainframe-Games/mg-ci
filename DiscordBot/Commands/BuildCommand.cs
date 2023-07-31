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
				}
			};
			var res = await Web.SendAsync(HttpMethod.Post, DiscordWrapper.Config.BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(command.User, "Build Started", res.Content);
			return new CommandResponse("Build Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return new CommandResponse("Build Server request failed", e.Message, true);
		}
	}

	// TODO: implement choosing targets. Currently there is no way to choose multiple options in Discord... I think web interface is only way forward with this approach
	// public override async Task ModifyOptions(SocketSlashCommand slashCommand)
	// {
	// 	// Get the user's choice
	// 	var selectedChoice = slashCommand.Data.Options.First().Value.ToString();
	// 	var workspace = DiscordWrapper.Config.Workspaces.First(x => x.Name == selectedChoice);
	//
	// 	var menuBuilder = new SelectMenuBuilder()
	// 		.WithPlaceholder("Select targets to build")
	// 		.WithCustomId("targets")
	// 		.WithMinValues(1)
	// 		.WithMaxValues(1)
	// 		.AddOption("All", "all", "All the build targets", isDefault: true);
	//
	// 	foreach (var target in workspace.Targets)
	// 		menuBuilder.AddOption(target, target, $"Build for `{target}`");
	//
	// 	var component = new ComponentBuilder()
	// 		.WithSelectMenu(menuBuilder)
	// 		.Build();
	//
	// 	try
	// 	{
	// 		// Modify the original response to update the options
	// 		await slashCommand.ModifyOriginalResponseAsync(properties =>
	// 		{
	// 			properties.Content = "Select build targets to build";
	// 			properties.Components = new Optional<MessageComponent>(component);
	// 		});
	// 	}
	// 	catch (Exception e)
	// 	{
	// 		await slashCommand.RespondError(e.GetType().Name, e.Message);
	// 	}
	// }

	private static SlashCommandOptionBuilder BuildArgumentsOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("args")
			.WithDescription("Arguments send to build server")
			.WithType(ApplicationCommandOptionType.String);
		return opt;
	}
}