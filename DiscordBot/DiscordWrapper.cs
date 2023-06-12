using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Configs;
using SharedLib;

namespace DiscordBot;

/// <summary>
/// Discord.NET Docs: https://discordnet.dev/guides/int_basics/application-commands/intro.html 
/// Web API Docs: https://discord.com/developers/docs/interactions/application-commands#slash-commands
/// </summary>
public class DiscordWrapper
{
	private readonly DiscordSocketClient _client;
	private readonly Configs.DiscordConfig _config;

	public DiscordWrapper(Configs.DiscordConfig config)
	{
		var socketConfig = new DiscordSocketConfig { UseInteractionSnowflakeDate = false };
		_client = new DiscordSocketClient(socketConfig);
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
		for (int i = 0; i < _config.WorkspaceNames.Count; i++)
			opt.AddChoice(_config.WorkspaceNames[i], i);
		return opt;
	}
	
	private SlashCommandOptionBuilder BuildArgumentsOptions()
	{
		var opt = new SlashCommandOptionBuilder()
			.WithName("args")
			.WithDescription("Arguments send to build server")
			// TODO: create dynamic way of adding args from master server
			// .AddChoice("-noprebuild", 0)
			// .AddChoice("-nobuild", 1)
			// .AddChoice("-nopostbuild", 2)
			// .AddChoice("-nosteamdeploy", 3)
			.WithType(ApplicationCommandOptionType.String);
		return opt;
	}

	private async Task ClientReady()
	{
		var guild = _client.GetGuild(_config.GuildId);

		var cmd = new SlashCommandBuilder()
			.WithName(_config.CommandName)
			.WithDescription("Starts a build from discord")
			.AddOptions(WorkspaceOptions())
			.AddOptions(BuildArgumentsOptions());

		var built = cmd.Build();

		try
		{
			// clear currents
			await FlushCommandsAsync(_config.GuildId);
			
			// guild only
			await guild.CreateApplicationCommandAsync(built);
		}
		catch (HttpException exception)
		{
			var json = Json.Serialise(exception.Message);
			Logger.Log(json);
		}
	}
	
	private async Task FlushCommandsAsync(params ulong[] guilds)
	{
		/*
		 * Note: Currently there aren't any global commands 
		 */
		
		// globals
		var cmds = await _client.GetGlobalApplicationCommandsAsync();
		foreach (var c in cmds)
			await c.DeleteAsync();
		
		// guilds
		foreach (var guildId in guilds)
		{
			var guild = _client.GetGuild(guildId);
			await guild.DeleteApplicationCommandsAsync();
		}
	}

	private async Task SlashCommandHandler(SocketSlashCommand command)
	{
		// We need to extract the user parameter from the command. since we only have one option and it's required, we can just use the first option.
		var user = (SocketGuildUser)command.User;
		await _config.RefreshAsync();

		if (!IsAuthorised(user))
		{
			await command.RespondError(user, "Error", "You are not authorised for this command");
			return;
		}

		string? workspaceName = null;
		string? args = null;

		// user options
		foreach (var option in command.Data.Options)
		{
			switch (option.Name)
			{
				case "workspaces":
					var index = (int)(long)option.Value;
					workspaceName = _config.WorkspaceNames[index];
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
			var res = await Web.SendAsync(HttpMethod.Post, _config.BuildServerUrl, body: body);
			await command.RespondSuccessDelayed(user, "Build Started", res.Content);
		}
		catch (Exception e)
		{
			Logger.Log(e);
			await command.RespondErrorDelayed(user, "Build Server request failed", e.Message);
		}
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