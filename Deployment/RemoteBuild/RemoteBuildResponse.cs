namespace Deployment.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public RemoteBuildTargetRequest? Request { get; set; }
	public string? BuildId { get; set; }
	public string? Base64 { get; set; }
	public string? Error { get; set; }
	
	public async Task<string> ProcessAsync()
	{
		if (string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (BuildPipeline.Current == null)
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active");
		
		Console.WriteLine($"Received build back from offload '{BuildId}'");
		await BuildPipeline.Current.RemoteBuildReceived(this);
		return "ok";
	}
}