using Deployment;
using Deployment.Server;
using Newtonsoft.Json;
using SharedLib;

namespace Server.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public string? BuildId { get; set; }
	public string? BuildPath { get; set; }
	[JsonIgnore] public byte[]? Data { get; set; }
	public string? Error { get; set; }

	public ServerResponse Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (BuildPipeline.Current == null)
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active");
		
		Logger.Log($"BuildId: {BuildId}, {Data?.ToMegaByteString()}");
		BuildPipeline.Current.RemoteBuildReceived(BuildId, BuildPath, Data).FireAndForget();
		return ServerResponse.Default;
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