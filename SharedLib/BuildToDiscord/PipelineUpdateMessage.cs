namespace SharedLib.BuildToDiscord;

public class PipelineUpdateMessage
{
	public ulong MessageId { get; set; }
	public PipelineReport? Report { get; set; }
}