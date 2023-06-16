using Discord;
using Discord.WebSocket;
using DiscordBot.Configs;
using SharedLib;

namespace DiscordBot.Commands;

public class BuildCommand : Command
{
	private string? BuildServerUrl { get; }
	private List<string?>? WorkspaceNames { get; }
	
	public BuildCommand(string? commandName, string? description, string buildServerUrl, List<string?>? workspaceNames) : base(commandName, description)
	{
		BuildServerUrl = buildServerUrl;
		WorkspaceNames = workspaceNames;
	}
	
	public override SlashCommandProperties Build()
	{
		return CreateCommand()
			.AddOptions(WorkspaceOptions())
			.AddOptions(BuildArgumentsOptions())
			.Build();
	}

	public override async Task ExecuteAsync(SocketSlashCommand command)
	{
		var workspaceName = GetOptionValueString(command, "workspace");
		var args = GetOptionValueString(command, "args");

		if (string.IsNullOrEmpty(workspaceName))
		{
			await command.RespondError(command.User, "Error", "No Workspace chosen");
			return;
		}

		await command.DeferAsync();

		try
		{
			// request to build server
			var body = new BuildRequest { WorkspaceBuildRequest = new WorkspaceReq { WorkspaceName = workspaceName, Args = args } };
			var res = await Web.SendAsync(HttpMethod.Post, BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(command.User, "Build Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await command.RespondErrorDelayed(command.User, "Build Server request failed", e.Message);
		}
	}
	
	private SlashCommandOptionBuilder WorkspaceOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("workspace")
			.WithDescription("List of available workspaces")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String);
		
		foreach (var workspace in WorkspaceNames)
			opt.AddChoice(workspace, workspace);

		return opt;
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