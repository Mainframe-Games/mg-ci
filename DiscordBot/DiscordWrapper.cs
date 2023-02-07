using System.Net;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Configs;
using Newtonsoft.Json.Linq;
using SharedLib;

namespace DiscordBot;

public class DiscordWrapper
{
	private readonly DiscordSocketClient _client;
	private readonly Configs.DiscordConfig _config;

	public DiscordWrapper(Configs.DiscordConfig config)
	{
		_client = new DiscordSocketClient();
		_client.Log += OnLog;
		_client.Ready += ClientReady;
		_client.SlashCommandExecuted += SlashCommandHandler;
		_config = config;
	}

	public async Task Init()
	{
		await _client.LoginAsync(TokenType.Bot, _config.Token);
		await _client.StartAsync();
		await Task.Delay(-1);
	}

	private SlashCommandOptionBuilder WorkspaceOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("workspaces")
			.WithDescription("List of available workspaces")
			.WithType(ApplicationCommandOptionType.Integer);
		for (int i = 0; i < _config.Workspaces.Count; i++)
			opt.AddChoice(_config.Workspaces[i].WorkspaceName, i);
		return opt;
	}
	
	private SlashCommandOptionBuilder BuildArgumentsOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("build-args")
			.WithDescription("Arguments send to build server")
			.AddChoice("-noprebuild", 0)
			.AddChoice("-nobuild", 1)
			.AddChoice("-nopostbuild", 2)
			.AddChoice("-nosteamdeploy", 3)
			.WithType(ApplicationCommandOptionType.String);
		return opt;
	}

	private async Task ClientReady()
	{
		// Let's build a guild command! We're going to need a guild so lets just put that in a variable.
		var guild = _client.GetGuild(_config.GuildId);

		var cmd = new SlashCommandBuilder()
			.WithName(_config.CommandName)
			.WithDescription("Starts a build from discord")
			.AddOption(WorkspaceOptions())
			.AddOption(BuildArgumentsOptions());
		
		try
		{
			await guild.CreateApplicationCommandAsync(cmd.Build());
		}
		catch (HttpException exception)
		{
			var json = Json.Serialise(exception.Message);
			Logger.Log(json);
		}
	}

	private async Task SlashCommandHandler(SocketSlashCommand command)
	{
		// We need to extract the user parameter from the command. since we only have one option and it's required, we can just use the first option.
		var user = (SocketGuildUser)command.User;
		_config.Refresh();

		if (!IsAuthorised(user))
		{
			await command.RespondError(user, "Error", "You are not authorised for this command");
			return;
		}

		string? workspaceName = null;
		string[]? args = null;

		// if no commands use channel id
		if (command.Data.Options.Count == 0 && !TryGetWorkspaceName(command.Channel.Id, out workspaceName))
		{
			await command.RespondError(user, "Error", "This channel is not configured with a workspace");
			return;
		}
		
		// user options
		foreach (var option in command.Data.Options)
		{
			switch (option.Name)
			{
				case "workspaces":
					var index = (int)(long)option.Value;
					workspaceName = _config.Workspaces[index].WorkspaceName;
					break;
				
				case "build-args":
					args = option.Value?.ToString()?.Split(' ');
					break;
			}
		}

		await command.DeferAsync(true);

		// request to build server
		var req = new BuildRequest { WorkspaceBuildRequest = new WorkspaceReq { WorkspaceName = workspaceName, Args = args } };
		var res = await Web.SendAsync(HttpMethod.Post, _config.BuildServerUrl, command.User.Id.ToString(), req);
		if (res.StatusCode != HttpStatusCode.OK)
		{
			await command.RespondErrorDelayed(user, "Build Server request failed", $"{res.StatusCode}: {res.Reason}\n{res.Content}");
			return;
		}

		// success
		var resData = JObject.Parse(res.Content)["data"]?.ToString();
		await command.RespondSuccessDelayed(user, "Build Started", resData ?? "Unknown Workspace");
	}

	private bool TryGetWorkspaceName(ulong channelId, out string? workspaceName)
	{
		foreach (var channel in _config.Workspaces)
		{
			if (channel.ChannelId != channelId) 
				continue;
			
			workspaceName = channel.WorkspaceName;
			return true;
		}

		workspaceName = null;
		return false;
	}
	
	private bool IsAuthorised(SocketGuildUser guildUser)
	{
		var roles = guildUser.Roles.Select(x => x.Name);
		
		foreach (var role in roles)
		{
			foreach (var authorisedRole in _config.AuthorisedRoles)
			{
				if (authorisedRole.Equals(role, StringComparison.CurrentCultureIgnoreCase))
					return true;
			}
		}

		return false;
	}

	private static async Task OnLog(LogMessage log)
	{
		Logger.Log(log.ToString());
		await Task.CompletedTask;
	}
	
	
}