using Discord;
using Discord.WebSocket;
using SharedLib.BuildToDiscord;

namespace DiscordBot;

public class MessageUpdater
{
	private readonly SocketTextChannel _channel;
	private readonly ulong _messageId;

	public MessageUpdater(BaseSocketClient client, ulong channelId, ulong messageId)
	{
		_channel = (SocketTextChannel)client.GetChannel(channelId);
		_messageId = messageId;
	}

	public async Task UpdateMessageAsync(PipelineReport report)
	{
		var message = await _channel.GetMessageAsync(_messageId);
		var originalEmbed = message.Embeds.ElementAt(0);
		var embeds = new List<Embed>
		{
			originalEmbed.UpdateEmbed(includeAuthor: true),
			BuildEmbedFromReport(report),
		};

		if (report.IsSuccessful)
			embeds.Add(BuildChangeLog(report));

		await _channel.ModifyMessageAsync(_messageId,
			properties => { properties.Embeds = new Optional<Embed[]>(embeds.ToArray()); });
	}

	private static Embed BuildEmbedFromReport(PipelineReport report)
	{
		var embed = new EmbedBuilder();

		embed.WithTitle(report.WorkspaceName);
		embed.WithUrl(report.TitleUrl);
		embed.WithThumbnailUrl(report.ThumbnailUrl);
		embed.WithColor(GetColor(report));
		embed.WithDescription(report.BuildDescription());

		foreach (var buildTarget in report.BuildTargetFields())
		{
			var field = new EmbedFieldBuilder { Name = buildTarget.Key, Value = buildTarget.Value, IsInline = true };
			embed.AddField(field);
		}

		embed.WithFooter("Last Updated");
		embed.WithCurrentTimestamp();
		
		return embed.Build();
	}

	private static Embed BuildChangeLog(PipelineReport report)
	{
		var embed = new EmbedBuilder();
		embed.WithTitle(report.ChangeLogTitle);
		embed.WithDescription(report.ChangeLogMessage);
		embed.WithColor(GetColor(report));
		return embed.Build();
	}

	private static Color GetColor(PipelineReport report)
	{
		if (report.IsFailed)
			return Color.Red;
		
		return report.IsSuccessful
			? Color.Green
			: Color.Blue;
	}
}