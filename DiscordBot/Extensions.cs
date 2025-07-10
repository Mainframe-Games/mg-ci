using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordBot;

public static class Extensions
{
	public const int MAX_MESSAGE_SIZE = 4096;
	
	public static async Task<RestInteractionMessage> RespondSuccessDelayed(this SocketSlashCommand command, IUser user, string title, string description)
	{
		if (description.Length > MAX_MESSAGE_SIZE)
			return await RespondSuccessFileDelayed(command, user, title, description);

		return await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(user, title, description, Color.Green);
		});
	}
	
	public static async Task<RestInteractionMessage?> RespondSuccessDelayed(this SocketSlashCommand command, IUser user, Embed embed, string? message = null)
	{
		if (embed.Description.Length > MAX_MESSAGE_SIZE)
			return await RespondSuccessFileDelayed(command, user, embed.Title, embed.Description);

		return await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Content = new Optional<string>(message ?? "");
			properties.Embed = embed;
		});
	}
	
	public static async Task<RestInteractionMessage?> RespondSuccessFileDelayed(this SocketInteraction command, IUser user, string title, string description)
	{
		var attachments = BuildAttachments(description);
		return await command.ModifyOriginalResponseAsync(properties =>
		{
			properties.Embed = CreateEmbed(user, title, null, Color.Green);
			properties.Attachments = attachments;
		});
	}

	public static Optional<IEnumerable<FileAttachment>> BuildAttachments(params string[] largeDescriptions)
	{
		var list = new List<FileAttachment>();
		
		for (var i = 0; i < largeDescriptions.Length; i++)
		{
			var description = largeDescriptions[i];
			var filePath = Path.Combine(Environment.CurrentDirectory, $"large_message_{i}.txt");
			File.WriteAllText(filePath, description);

			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists)
				throw new FileNotFoundException($"File not found at: {filePath}");
			
			list.Add(new FileAttachment(fileInfo.FullName, fileInfo.Name));
		}

		return new Optional<IEnumerable<FileAttachment>>(list);
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

	public static Embed BuildEmbed(this SharedLib.Webhooks.Discord.Embed template)
	{
		var embed = new EmbedBuilder();

		if (template.AuthorName is not null && template.AuthorIconUrl is not null)
			embed.WithAuthor(template.AuthorName, template.AuthorIconUrl);
		else if (template.AuthorName is not null)
			embed.WithAuthor(template.AuthorName);
		
		if (template.Title is not null)
			embed.WithTitle(template.Title);
		if (template.Description is not null)
			embed.WithDescription(template.Description);
		if (template.Colour is not null)
			embed.WithColor(GetColorFromColour(template.Colour));
		if (template.Url is not null)
			embed.WithUrl(template.Url);
		if (template.ThumbnailUrl is not null)
			embed.WithThumbnailUrl(template.ThumbnailUrl);
		
		if (template.Fields is not null)
		{
			foreach (var field in template.Fields)
			{
				embed.AddField(new EmbedFieldBuilder
				{
					Name = field.Name,
					Value = field.Value,
					IsInline = true
				});
			}
		}

		if (template.IncludeTimeStamp is true)
		{
			embed.WithFooter("Last Updated");
			embed.WithCurrentTimestamp();
		}
		
		return embed.Build();
	}

	private static Color GetColorFromColour(SharedLib.Webhooks.Discord.Colour? templateColour)
	{
		return templateColour switch
		{
			SharedLib.Webhooks.Discord.Colour.DEFAULT => Color.Default,
			SharedLib.Webhooks.Discord.Colour.AQUA => Color.Blue,
			SharedLib.Webhooks.Discord.Colour.DARK_AQUA => Color.DarkBlue,
			SharedLib.Webhooks.Discord.Colour.GREEN => Color.Green,
			SharedLib.Webhooks.Discord.Colour.DARK_GREEN => Color.DarkGreen,
			SharedLib.Webhooks.Discord.Colour.BLUE => Color.Blue,
			SharedLib.Webhooks.Discord.Colour.DARK_BLUE => Color.DarkBlue,
			SharedLib.Webhooks.Discord.Colour.PURPLE => Color.Purple,
			SharedLib.Webhooks.Discord.Colour.DARK_PURPLE => Color.DarkPurple,
			SharedLib.Webhooks.Discord.Colour.LUMINOUS_VIVID_PINK => Color.Magenta,
			SharedLib.Webhooks.Discord.Colour.DARK_VIVID_PINK => Color.DarkMagenta,
			SharedLib.Webhooks.Discord.Colour.GOLD => Color.Gold,
			SharedLib.Webhooks.Discord.Colour.DARK_GOLD => Color.Gold,
			SharedLib.Webhooks.Discord.Colour.ORANGE => Color.Orange,
			SharedLib.Webhooks.Discord.Colour.DARK_ORANGE => Color.DarkOrange,
			SharedLib.Webhooks.Discord.Colour.RED => Color.Red,
			SharedLib.Webhooks.Discord.Colour.DARK_RED => Color.DarkRed,
			SharedLib.Webhooks.Discord.Colour.GREY => Color.LightGrey,
			SharedLib.Webhooks.Discord.Colour.DARK_GREY => Color.DarkGrey,
			SharedLib.Webhooks.Discord.Colour.DARKER_GREY => Color.DarkerGrey,
			SharedLib.Webhooks.Discord.Colour.LIGHT_GREY => Color.LightGrey,
			SharedLib.Webhooks.Discord.Colour.NAVY => Color.Blue,
			SharedLib.Webhooks.Discord.Colour.DARK_NAVY => Color.DarkBlue,
			SharedLib.Webhooks.Discord.Colour.YELLOW => Color.Gold,
			_ => Color.Default
		};
	}
}