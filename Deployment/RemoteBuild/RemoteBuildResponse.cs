using Deployment.Misc;
using Newtonsoft.Json;
using SharedLib;

namespace Deployment.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public string? BuildId { get; set; }
	public string? BuildPath { get; set; }
	[JsonIgnore] public byte[]? Data { get; set; }
	public string? Error { get; set; }

	public string Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (BuildPipeline.Current == null)
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active");
		
		Logger.Log($"BuildId: {BuildId}, {Data?.ToMegaByteString()}");
		BuildPipeline.Current.Unity.RemoteBuildReceived(BuildId, BuildPath, Data).FireAndForget();
		
		return "ok";
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