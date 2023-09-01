namespace SharedLib.BuildToDiscord;

/// <summary>
/// Used for sending packets to Discord listen server
/// </summary>
public class DiscordServerPacket
{
	public PipelineUpdateMessage? PipelineUpdate { get; set; }
}