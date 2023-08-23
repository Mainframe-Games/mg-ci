using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands;

public class ReminderRemoveCommand : Command
{
	public override string? CommandName => "reminder-remove";
	public override string? Description => "Removes reminder event";

	public override SlashCommandProperties Build()
	{
		var choices = DiscordWrapper.Config.Reminders?.Select(x => x.Name) ?? Array.Empty<string>();

		return CreateCommand()
			.AddOption(BuildOptionStringWithChoices("reminder", "Reminder to remove", true, choices))
			.Build();
	}

	public override async Task<CommandResponse> ExecuteAsync(SocketSlashCommand command)
	{
		var reminderName = GetOptionValueString(command, "reminder");
		await DiscordWrapper.Instance.RemoveReminderAsync(reminderName);
		return new CommandResponse("Removed Reminder", $"Reminder: {reminderName}");
	}
}