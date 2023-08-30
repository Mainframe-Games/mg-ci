using Discord;
using Discord.WebSocket;

namespace DiscordBot;

public class MessageUpdater
{
	private readonly SocketTextChannel _channel;
	private readonly ulong _messageId;
	private readonly List<BuildSteps> _buildTasks;

	public bool IsPending => _buildTasks.Any(x => x.State is BuildTaskState.Pending);
	public bool IsFailed => _buildTasks.Any(x => x.State is BuildTaskState.Failed);
	public bool IsSuccessful => _buildTasks.All(x => x.State is BuildTaskState.Succeed);

	public MessageUpdater(BaseSocketClient client, ulong channelId, ulong messageId, params string[] targets)
	{
		_channel = (SocketTextChannel)client.GetChannel(channelId);
		_messageId = messageId;

		var targs = targets.Select(x => new BuildSteps(x)).ToArray();
		_buildTasks = new List<BuildSteps>
		{
			new("Pre Build"),
			new("Build Targets", targs),
			new("Deploy"),
			new("Post Build"),
		};
	}

	public async Task UpdateMessageAsync()
	{
		var message = await _channel.GetMessageAsync(_messageId);
		var description = BuildSteps.BuildEmbedFields(_buildTasks);

		Color colour;

		if (IsFailed)
			colour = Color.Red;
		else if (IsSuccessful)
			colour = Color.Green;
		else
			colour = Color.Blue;

		var embed = message.Embeds.ElementAt(0);
		var embeds = new[]
		{
			embed.UpdateEmbed(),
			embed.UpdateEmbed(
				includeAuthor: false,
				title: "Live Updates",
				description: description,
				color: colour,
				includeTimeStamp: true,
				fields: _buildTasks[1].GetSubTaskEmbedFields()),
		};

		await _channel.ModifyMessageAsync(_messageId,
			properties => { properties.Embeds = new Optional<Embed[]>(embeds); });
	}

	public void FakeIncomingUpdate()
	{
		foreach (var task in _buildTasks)
		{
			var nextTask = GetNextPendingStep(task);

			if (nextTask == null)
				continue;

			nextTask.State = BuildTaskState.Succeed;
			break;
		}
	}

	private static BuildSteps? GetNextPendingStep(BuildSteps task)
	{
		if (task.State is BuildTaskState.Pending)
			return task;

		foreach (var subTask in task.SubTasks)
		{
			var ne = GetNextPendingStep(subTask);

			if (ne != null)
				return ne;
		}

		return null;
	}
}