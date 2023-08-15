using Deployment;
using Deployment.Server;
using Newtonsoft.Json;
using SharedLib;

namespace Server.RemoteBuild;

public class RemoteBuildResponse : IRemoteControllable
{
	public ulong PipelineId { get; set; }
	public string? BuildIdGuid { get; set; }
	public string? BuildPath { get; set; }
	public string? Error { get; set; }
	[JsonIgnore] public FilePacker.Entry[] Data { get; set; }

	public ServerResponse Process()
	{
		if (!string.IsNullOrEmpty(Error))
			throw new Exception(Error);

		if (!App.Pipelines.TryGetValue(PipelineId, out var buildPipeline))
			throw new NullReferenceException($"{nameof(BuildPipeline)} is not active. Id: {PipelineId}");
		
		Logger.Log($"BuildId: {BuildIdGuid}, {Data?.ToByteSizeString()}");
		
		buildPipeline.RemoteBuildReceived(BuildIdGuid, BuildPath, Data).FireAndForget();
		return ServerResponse.Default;
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(PipelineId);
		writer.Write(BuildIdGuid ?? string.Empty);
		writer.Write(BuildPath ?? string.Empty);
		writer.Write(Error ?? string.Empty);

		writer.Write(Data.Length);
		for (int i = 0; i < Data.Length; i++)
			Data[i].Write(writer);
	}
	
	public void Read(BinaryReader reader)
	{
		PipelineId = reader.ReadUInt64();
		BuildIdGuid = reader.ReadString();
		BuildPath = reader.ReadString();
		Error = reader.ReadString();

		var length = reader.ReadInt32();
		Data = new FilePacker.Entry[length];
		for (int i = 0; i < length; i++)
		{
			Data[i] = new FilePacker.Entry();
			Data[i].Read(reader);
		}
	}
}