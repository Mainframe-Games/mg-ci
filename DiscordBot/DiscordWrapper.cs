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
	public static DiscordWrapper Instance { get; private set; }
	public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
	public static DiscordConfig Config { get; private set; }
	
	private readonly DiscordSocketClient _client;
	private Command[] Commands { get; set; }

	private readonly Dictionary<string, TimeEvent> _reminders = new();

	public DiscordWrapper(DiscordConfig config)
	{
		Instance = this;
		
		var socketConfig = new DiscordSocketConfig { UseInteractionSnowflakeDate = false };
		_client = new DiscordSocketClient(socketConfig);
		_client.Log += OnLog;
		_client.Ready += ClientReady;
		_client.SlashCommandExecuted += SlashCommandHandler;
		_client.InteractionCreated += HandleInteractionAsync;
		_client.SelectMenuExecuted += SelectMenuExecutedAsync;
		Config = config;

		RefreshReminders();
		
		RefreshCommand.OnRefreshed += RefreshCommands;
	}

	private async void OnEventTriggered(Reminder reminder)
	{
		try
		{
			// Get the role by name
			var channel = (SocketTextChannel)_client.GetChannel(reminder.ChannelId);
			await channel.SendMessageAsync(reminder.Message);
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
		
		foreach (var cmd in Commands)
			await RefreshCommandAsync(cmd);
	}

	private async Task RefreshCommandAsync(Command command)
	{
		try
		{
			Logger.Log($"Creating command: {command.CommandName}...");
			var guild = _client.GetGuild(Config.GuildId);
			await guild.CreateApplicationCommandAsync(command.Build());
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

	private void RefreshReminders()
	{
		if (Config.Reminders == null) 
			return;

		foreach (var reminder in _reminders)
			reminder.Value.Stop();
		
		_reminders.Clear();
		
		foreach (var reminder in Config.Reminders)
		{
			Logger.Log($"Registering Reminder: {reminder}");
			
			var timer = new TimeEvent(reminder.Hour, reminder.Minute);
			timer.OnEventTriggered += () => OnEventTriggered(reminder);
			
			_reminders.Add(reminder.Name, timer);
		}
	}

	private static async Task OnLog(LogMessage log)
	{
		Logger.Log(log.ToString());
		await Task.CompletedTask;
	}

	public async Task AddReminderAsync(Reminder newReminder)
	{
		Config.Reminders?.Add(newReminder);
		await Config.SaveAsync();
		
		RefreshReminders();

		if (TryGetCommand<ReminderRemoveCommand>(out var removeCommand))
			await RefreshCommandAsync(removeCommand);
	}
	
	public async Task RemoveReminderAsync(string? reminderName)
	{
		if (!_reminders.TryGetValue(reminderName, out var timeEvent))
			return;
		
		timeEvent.Stop();

		for (int i = 0; i < Config.Reminders.Count; i++)
		{
			if (Config.Reminders[i].Name != reminderName)
				continue;
			
			Config.Reminders.RemoveAt(i);
			await Config.SaveAsync();
			break;
		}

		RefreshReminders();

		if (TryGetCommand<ReminderRemoveCommand>(out var removeCommand))
			await RefreshCommandAsync(removeCommand);
	}

	public bool TryGetCommand<T>(out T command) where T : Command
	{
		foreach (var cmd in Commands)
		{
			if (cmd is not T castedCmd) 
				continue;
			
			command = castedCmd;
			return true;
		}

		command = null;
		return false;
	}
}