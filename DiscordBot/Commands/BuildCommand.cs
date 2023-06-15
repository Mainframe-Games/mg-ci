using Discord;
using Discord.WebSocket;
using DiscordBot.Configs;
using SharedLib;

namespace DiscordBot.Commands;

public class BuildCommand : Command
{
	public string? BuildServerUrl { get; set; }
	public List<string?>? WorkspaceNames { get; set; }
	
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

	public override async Task ExecuteAsync(SocketSlashCommand command, IUser user)
	{
		string? workspaceName = null;
		string? args = null;

		// user options
		foreach (var option in command.Data.Options)
		{
			switch (option.Name)
			{
				case "workspaces":
					var index = (int)(long)option.Value;
					workspaceName = WorkspaceNames[index];
					break;

				case "args":
					args = option.Value?.ToString();
					break;
			}
		}

		if (string.IsNullOrEmpty(workspaceName))
		{
			await command.RespondError(user, "Error", "No Workspace chosen");
			return;
		}

		await command.DeferAsync(/*true,*/);

		try
		{
			// request to build server
			var body = new BuildRequest { WorkspaceBuildRequest = new WorkspaceReq { WorkspaceName = workspaceName, Args = args } };
			var res = await Web.SendAsync(HttpMethod.Post, BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(user, "Build Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await command.RespondErrorDelayed(user, "Build Server request failed", e.Message);
		}
	}
	
	private SlashCommandOptionBuilder WorkspaceOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("workspaces")
			.WithDescription("List of available workspaces")
			.WithType(ApplicationCommandOptionType.Integer);
		for (int i = 0; i < WorkspaceNames.Count; i++)
			opt.AddChoice(WorkspaceNames[i], i);
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