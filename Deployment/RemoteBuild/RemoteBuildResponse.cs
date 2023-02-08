using SharedLib;

namespace Deployment.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public RemoteBuildTargetRequest? Request { get; set; }
	public string? BuildId { get; set; }
	public string? Base64 { get; set; }
	public string? Error { get; set; }
	
	public async Task<string> ProcessAsync()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		UnpackBuild();
		await Task.CompletedTask;
		return "ok";
	}

	private void UnpackBuild()
	{
		if (BuildPipeline.Current == null)
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active");
		
		Logger.Log($"Received build back from offload '{BuildId}'");
		Task.Run(() => BuildPipeline.Current.RemoteBuildReceived(this));
	}
}