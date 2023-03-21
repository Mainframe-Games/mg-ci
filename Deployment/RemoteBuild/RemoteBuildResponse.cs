using Newtonsoft.Json;
using SharedLib;

namespace Deployment.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public string? BuildId { get; set; }
	public string? BuildPath { get; set; }
	[JsonIgnore] public byte[]? Data { get; set; }
	public string? Error { get; set; }

	private Thread? _thread;
	
	public async Task<string> ProcessAsync()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		_thread = new Thread(UnpackBuild);
		_thread.Start();
		await Task.CompletedTask;
		return "ok";
	}

	private async void UnpackBuild()
	{
		if (BuildPipeline.Current == null)
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active");
		Logger.Log($"Received build back from offload '{BuildId}'");
		
		Logger.Log($"BuildId: {BuildId}, {Data?.ToMegaByteString()}");
		await BuildPipeline.Current.Unity.RemoteBuildReceived(BuildId, BuildPath, Data);
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(BuildId ?? string.Empty);
		writer.Write(BuildPath ?? string.Empty);
		writer.Write(Data?.Length ?? 0);
		writer.Write(Data ?? Array.Empty<byte>());
		writer.Write(Error ?? string.Empty);
	}
	
	public void Read(BinaryReader reader)
	{
		BuildId = reader.ReadString();
		BuildPath = reader.ReadString();
		var length = reader.ReadInt32();
		Data = reader.ReadBytes(length);
		Error = reader.ReadString();
	}
}