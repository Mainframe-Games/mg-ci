using Deployment;
using Deployment.Server;
using Newtonsoft.Json;
using SharedLib;

namespace Server.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public ulong PipelineId { get; set; }
	public string? BuildId { get; set; }
	public string? BuildPath { get; set; }
	[JsonIgnore] public byte[]? Data { get; set; }
	public string? Error { get; set; }

	public ServerResponse Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (!App.Pipelines.TryGetValue(PipelineId, out var buildPipeline))
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active. Id: {PipelineId}");
		
		Logger.Log($"BuildId: {BuildId}, {Data?.ToMegaByteString()}");
		
		buildPipeline.RemoteBuildReceived(BuildId, BuildPath, Data).FireAndForget();
		return ServerResponse.Default;
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(PipelineId);
		writer.Write(BuildId ?? string.Empty);
		writer.Write(BuildPath ?? string.Empty);
		writer.Write(Data?.Length ?? 0);
		writer.Write(Data ?? Array.Empty<byte>());
		writer.Write(Error ?? string.Empty);
	}
	
	public void Read(BinaryReader reader)
	{
		PipelineId = reader.ReadUInt64();
		BuildId = reader.ReadString();
		BuildPath = reader.ReadString();
		var length = reader.ReadInt32();
		Data = reader.ReadBytes(length);
		Error = reader.ReadString();
	}
}