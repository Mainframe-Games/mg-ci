using Discord;
using Discord.WebSocket;

namespace DiscordBot;

public static class Extensions
{
	private const int MAX_MESSAGE_SIZE = 4096;
	
	public static async Task RespondSuccessDelayed(this SocketSlashCommand command, IUser user, string title, string description)
	{
		if (description.Length > MAX_MESSAGE_SIZE)
		{
			await RespondSuccessFileDelayed(command, user, title, description);
		}
		else
		{
			await command.ModifyOriginalResponseAsync(properties =>
			{
				properties.Embed = CreateEmbed(user, title, description, Color.Green);
			});
		}
	}
	
	private static async Task RespondSuccessFileDelayed(this SocketSlashCommand command, IUser user, string title, string description)
	{
		var filePath = Path.Combine(Environment.CurrentDirectory, "large_message.txt");
		await File.WriteAllTextAsync(filePath, description);
	
		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists)
			throw new FileNotFoundException($"File not found at: {filePath}");

		await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(user, title, null, Color.Green);
			properties.Attachments = new Optional<IEnumerable<FileAttachment>>(new List<FileAttachment>
			{
				new(fileInfo.FullName, fileInfo.Name)
			});
		});
	}
	
	public static async Task RespondError(this SocketSlashCommand command, string title, string description)
	{
		await command.RespondAsync(embed: CreateEmbed(command.User, title, description, Color.Red), ephemeral: true);
	}
	
	public static async Task RespondErrorDelayed(this SocketSlashCommand command, string title, string description)
	{
		await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(command.User, title, description, Color.Green);
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