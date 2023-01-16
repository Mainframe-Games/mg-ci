using Discord;
using Discord.WebSocket;

namespace DiscordBot;

public static class Extensions
{
	public static async Task RespondSuccess(this SocketSlashCommand command, IUser user, string title, string description)
	{
		await command.RespondAsync(embed: CreateEmbed(user, title, description, Color.Green), ephemeral: true);
	}
	
	public static async Task RespondSuccessDelayed(this SocketSlashCommand command, IUser user, string title, string description)
	{
		await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(user, title, description, Color.Green);
		});
	}
	
	public static async Task RespondError(this SocketSlashCommand command, IUser user, string title, string description)
	{
		await command.RespondAsync(embed: CreateEmbed(user, title, description, Color.Red), ephemeral: true);
	}
	
	public static async Task RespondErrorDelayed(this SocketSlashCommand command, IUser user, string title, string description)
	{
		await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(user, title, description, Color.Green);
		});
	}

	private static Embed CreateEmbed(IUser? user = null, string? title = null, string? description = null,
		Color? color = null, bool? includeTimeStamp = null)
	{
		var embed = new EmbedBuilder();

		if (user != null)
			embed.WithAuthor(user.ToString(), user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
		if (title != null)
			embed.WithTitle(title);
		if (description != null)
			embed.WithDescription(description);
		if (color != null)
			embed.WithColor((Color)color);
		if (includeTimeStamp != null && (bool)includeTimeStamp)
			embed.WithCurrentTimestamp();

		return embed.Build();
	}
}