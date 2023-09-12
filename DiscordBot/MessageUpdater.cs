using Discord;
using Discord.WebSocket;
using SharedLib;
using SharedLib.BuildToDiscord;

namespace DiscordBot;

public class MessageUpdater
{
	private readonly SocketTextChannel _channel;
	private readonly ulong _messageId;
	private readonly WorkspaceMeta? _workspaceMeta;

	public MessageUpdater(BaseSocketClient client, ulong channelId, ulong messageId, WorkspaceMeta? workspaceMeta)
	{
		_channel = (SocketTextChannel)client.GetChannel(channelId);
		_messageId = messageId;
		_workspaceMeta = (WorkspaceMeta?)workspaceMeta?.Clone();
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

		if (report.IsSuccessful || report.IsFailed)
			embeds.Add(BuildChangeLog(report));

		await _channel.ModifyMessageAsync(_messageId,
			properties => { properties.Embeds = new Optional<Embed[]>(embeds.ToArray()); });
	}

	/// <summary>
	/// Src: https://discohook.org/
	/// </summary>
	private Embed BuildEmbedFromReport(PipelineReport report)
	{
		var embed = new EmbedBuilder();

		embed.WithTitle(_workspaceMeta?.ProjectName);
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

	/// <summary>
	/// Src: https://discohook.org/
	/// </summary>
	private Embed BuildChangeLog(PipelineReport report)
	{
		var embed = new EmbedBuilder();
		
		embed.WithTitle(report.CompleteTitle);
		embed.WithDescription(report.CompleteMessage);
		embed.WithColor(GetColor(report));
		
		if (_workspaceMeta?.Url is not null)
			embed.WithUrl(_workspaceMeta.Url);
		if (_workspaceMeta?.ThumbnailUrl is not null)
			embed.WithThumbnailUrl(_workspaceMeta.ThumbnailUrl);
		
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