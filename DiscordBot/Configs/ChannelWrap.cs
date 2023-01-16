namespace DiscordBot.Configs;

public class ChannelWrap
{
	/// <summary>
	/// Discord channel ID
	/// </summary>
	public ulong ChannelId { get; set; }

	/// <summary>
	/// Workspace name from Plastic SCM
	/// </summary>
	public string WorkspaceName { get; set; }
}