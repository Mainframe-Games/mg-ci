namespace SharedLib.BuildToDiscord;

public class PipelineUpdateMessage
{
	public ulong CommandId { get; set; }
	public PipelineReport? Report { get; set; }
}