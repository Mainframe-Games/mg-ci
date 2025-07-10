using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configs;

namespace DiscordBot.Commands;

public class ReminderAddCommand : Command
{
	public override string? CommandName => "reminder-add";
	public override string? Description => "Adds a new reminder event";
    
	public override SlashCommandProperties Build()
	{
		return CreateCommand()
			.AddOption(BuildOptionString("name", "Name of the reminder", true))
			.AddOption(BuildOptionNumber("hour", "The hour of event (24 hour time)", true))
			.AddOption(BuildOptionNumber("minute", "The minute of event (0 default)", false))
			.AddOption(BuildOptionNumber("message", "The message you want for the event", true))
			.AddOption(BuildOptionNumber("channel", "The channelId you want for the event to happen in", true))
			.Build();
	}

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		try
		{
			var name = GetOptionValueString(command, "name");
			var hour = GetOptionValueNumber(command, "hour", 0);
			var minute = GetOptionValueNumber(command, "minute", 0);
			var message = GetOptionValueString(command, "message");
			var channel = GetOptionValueNumber(command, "channel");

			if (DiscordWrapper.Config.Reminders?.Any(x => x.Name == name) is true)
				return new CommandResponse("Error", $"Reminder already exists: {name}", true);

			var newReminder = new Reminder
			{
				Name = name,
				Hour = (int)hour,
				Minute = (int)minute,
				ChannelId = (ulong)channel,
				Message = message
			};

			await DiscordWrapper.Instance.AddReminderAsync(newReminder);
			return new CommandResponse("New reminder added", newReminder.ToString());
		}
		catch (Exception e)
		{
			return new CommandResponse("Failed to add reminder", e.Message, true);
		}
	}
}