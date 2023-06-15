using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Commands;
using SharedLib;
using DiscordConfig = DiscordBot.Configs.DiscordConfig;

namespace DiscordBot;

/// <summary>
/// Discord.NET Docs: https://discordnet.dev/guides/int_basics/application-commands/intro.html 
/// Web API Docs: https://discord.com/developers/docs/interactions/application-commands#slash-commands
/// </summary>
public class DiscordWrapper
{
	private readonly DiscordSocketClient _client;
	private readonly DiscordConfig _config;
	
	private Command[] Commands { get; set; }

	public DiscordWrapper(DiscordConfig config)
	{
		var socketConfig = new DiscordSocketConfig { UseInteractionSnowflakeDate = false };
		_client = new DiscordSocketClient(socketConfig);
		_client.Log += OnLog;
		_client.Ready += ClientReady;
		_client.SlashCommandExecuted += SlashCommandHandler;
		_config = config;

		RefreshCommand.OnRefreshed += RefreshCommands;
	}

	public async Task Init()
	{
		await _client.LoginAsync(TokenType.Bot, _config.Token);
		await _client.StartAsync();
		await Task.Delay(-1);
	}

	private async Task ClientReady()
	{
		await RefreshCommands();
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

	private async Task RefreshCommands()
	{
		// clear currents
		await FlushCommandsAsync(_config.GuildId);

		Commands = new Command[]
		{
			new BuildCommand(_config.CommandName, "Starts a build from discord", _config.BuildServerUrl, _config.WorkspaceNames),
			new RefreshCommand("refresh-workspaces", "Refreshes to workspaces on the master server", _config),
			new ServerCommand("update-server-image", "Requests to master server to update game server images")
		};
		
		try
		{
			var guild = _client.GetGuild(_config.GuildId);
			foreach (var cmd in Commands)
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
		await _config.RefreshAsync();

		if (!IsAuthorised(user))
		{
			await command.RespondError(user, "Unauthorised", "You are not authorised for this command");
			return;
		}

		var cmd = Commands.FirstOrDefault(x => command.CommandName == x.CommandName);

		if (cmd != null)
		{
			await cmd.ExecuteAsync(command, user);
		}
		else
		{
			await command.RespondError(user, "Not Recognised", $"Command not recognised: {command.CommandName}");
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