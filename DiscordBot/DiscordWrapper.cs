using System.Reflection;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configs;
using SharedLib;
using DiscordConfig = DiscordBot.Configs.DiscordConfig;

namespace DiscordBot;

/// <summary>
/// Discord.NET Docs: https://discordnet.dev/guides/int_basics/application-commands/intro.html 
/// Web API Docs: https://discord.com/developers/docs/interactions/application-commands#slash-commands
/// </summary>
public class DiscordWrapper
{
	public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
	public static DiscordConfig Config { get; private set; }
	
	private readonly DiscordSocketClient _client;
	private Command[] Commands { get; set; }

	public DiscordWrapper(DiscordConfig config)
	{
		var socketConfig = new DiscordSocketConfig { UseInteractionSnowflakeDate = false };
		_client = new DiscordSocketClient(socketConfig);
		_client.Log += OnLog;
		_client.Ready += ClientReady;
		_client.SlashCommandExecuted += SlashCommandHandler;
		_client.InteractionCreated += HandleInteractionAsync;
		_client.SelectMenuExecuted += SelectMenuExecutedAsync;
		Config = config;

		RefreshCommand.OnRefreshed += RefreshCommands;
		
		// Reminders
		if (config.Reminders == null) 
			return;
		
		foreach (var reminder in config.Reminders)
		{
			Logger.Log($"Registering Reminder: {reminder}");
			var timer = new TimeEvent(reminder.Hour, reminder.Minute);
			timer.OnEventTriggered += () => OnEventTriggered(reminder);
		}
	}

	private async void OnEventTriggered(Reminder reminder)
	{
		try
		{
			// Get the role by name
			var guild = _client.GetGuild(Config.GuildId);
			var channel = (SocketTextChannel)_client.GetChannel(reminder.ChannelId);
			
			await channel.SendMessageAsync(
				$"{guild.EveryoneRole.Mention} {reminder.Name}",
				embed: Extensions.CreateEmbed(
					title: reminder.Name,
					description: reminder.Message,
					includeTimeStamp: true));
		}
		catch (Exception e)
		{
			Logger.Log(e);
		}
	}

	private async Task SelectMenuExecutedAsync(SocketMessageComponent interaction)
	{
		Console.WriteLine("------- SelectMenuExecutedAsync");
		await Task.CompletedTask;
	}

	private async Task HandleInteractionAsync(SocketInteraction interaction)
	{
		Console.WriteLine("------- HandleInteractionAsync");
		
		if (interaction is not SocketSlashCommand command)
			return;

		var cmd = Commands.FirstOrDefault(x => command.CommandName == x.CommandName);
		if (cmd != null)
			await cmd.ModifyOptions(command);
	}

	public async Task Init()
	{
		await _client.LoginAsync(TokenType.Bot, Config.Token);
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
		await FlushCommandsAsync(Config.GuildId);

		Commands = Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract)
			.Select(t => (Command)Activator.CreateInstance(t))
			.ToArray();
		
		try
		{
			var guild = _client.GetGuild(Config.GuildId);
			foreach (var cmd in Commands)
			{
				Logger.Log($"Creating command: {cmd.CommandName}...");
				await guild.CreateApplicationCommandAsync(cmd.Build());
			}
		}
		catch (HttpException exception)
		{
			var json = Json.Serialise(exception.Message);
			Logger.Log(json);
		}
	}

	private async Task SlashCommandHandler(SocketSlashCommand command)
	{
		// check auth
		if (!IsAuthorised((SocketGuildUser)command.User))
		{
			await command.RespondError("Unauthorised", "You are not authorised for this command");
			return;
		}

		// find command
		var cmd = Commands.FirstOrDefault(x => command.CommandName == x.CommandName);
		var commandFull = $"`/{command.CommandName} {string.Join(" ", command.Data?.Options?.Select(x => $"{x.Name} {x.Value}") ?? Array.Empty<string>())}`";
		
		// execute or fail
		if (cmd == null)
		{
			await command.RespondError("Not Recognised", $"Command not recognised: {command.CommandName}");
			return;
		}
		
		await command.DeferAsync();
		var res = await cmd.ExecuteAsync(command);
		var fullResponse = $"{commandFull}\n{res.Content}";

		if (res.IsError)
		{
			await command.RespondErrorDelayed(res.Title, fullResponse);
			return;
		}
		
		await command.RespondSuccessDelayed(command.User, res.Title, fullResponse);
	}

	private static bool IsAuthorised(SocketGuildUser guildUser)
	{
		var roles = guildUser.Roles.Select(x => x.Name);
		
		foreach (var role in roles)
		{
			foreach (var authorisedRole in Config.AuthorisedRoles)
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