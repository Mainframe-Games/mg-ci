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
		
		BuildPipeline.Current?.RemoteBuildReceived(this);
		await Task.CompletedTask;
		return "ok";
	}
}