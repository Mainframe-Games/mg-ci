using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordBot;

public static class Extensions
{
	private const int MAX_MESSAGE_SIZE = 4096;
	
	public static async Task<RestInteractionMessage?> RespondSuccessDelayed(this SocketSlashCommand command, IUser user, string title, string description)
	{
		if (description.Length > MAX_MESSAGE_SIZE)
			return await RespondSuccessFileDelayed(command, user, title, description);

		return await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(user, title, description, Color.Green);
		});
	}
	
	public static async Task<RestInteractionMessage?> RespondSuccessDelayed(this SocketSlashCommand command, IUser user, Embed embed)
	{
		if (embed.Description.Length > MAX_MESSAGE_SIZE)
			return await RespondSuccessFileDelayed(command, user, embed.Title, embed.Description);

		return await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = embed;
		});
	}
	
	private static async Task<RestInteractionMessage?> RespondSuccessFileDelayed(this SocketInteraction command, IUser user, string title, string description)
	{
		var filePath = Path.Combine(Environment.CurrentDirectory, "large_message.txt");
		await File.WriteAllTextAsync(filePath, description);
	
		var fileInfo = new FileInfo(filePath);
		if (!fileInfo.Exists)
			throw new FileNotFoundException($"File not found at: {filePath}");

		return await command.ModifyOriginalResponseAsync(properties =>
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
			properties.Embed = CreateEmbed(command.User, title, description, Color.Red);
		});
	}

	public static Embed CreateEmbed(IUser? user = null,
		string? title = null,
		string? description = null,
		Color? color = null,
		bool? includeTimeStamp = null,
		string? url = null,
		string? thumbnailUrl = null)
	{
		var embed = new EmbedBuilder();

		if (user is not null)
			embed.WithAuthor(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
		if (title is not null)
			embed.WithTitle(title);
		if (description is not null)
			embed.WithDescription(description);
		if (color is not null)
			embed.WithColor((Color)color);
		if (includeTimeStamp is true)
			embed.WithCurrentTimestamp();
		if (url is not null)
			embed.WithUrl(url);
		if (thumbnailUrl is not null)
			embed.WithThumbnailUrl(thumbnailUrl);
		return embed.Build();
	}
	
	public static Embed UpdateEmbed(this IEmbed originalEmbed, bool includeAuthor = false, string? title = null, 
		string? description = null, Color? color = null, bool? includeTimeStamp = null, 
		params EmbedFieldBuilder[] fields)
	{
		var embed = new EmbedBuilder();

		if (includeAuthor && originalEmbed.Author is not null)
			embed.WithAuthor(originalEmbed.Author.Value.Name, originalEmbed.Author.Value.IconUrl);
		
		embed.WithTitle(title ?? originalEmbed.Title);
		embed.WithDescription(description ?? originalEmbed.Description);
		embed.WithColor(color ?? originalEmbed.Color ?? Color.Default);

		foreach (var field in fields)
			embed.AddField(field);

		if (includeTimeStamp is true)
		{
			embed.WithFooter("Last Updated");
			embed.WithCurrentTimestamp();
		}
		
		return embed.Build();
	}
}