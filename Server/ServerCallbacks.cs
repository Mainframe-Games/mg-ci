using System.Net;
using Deployment;
using Server.Configs;
using Server.RemoteBuild;
using SharedLib.Server;

namespace Server;

public class ServerCallbacks : IServerCallbacks
{
	private readonly ServerConfig _config;
	ListenServer IServerCallbacks.Server { get; set; }
	
	public ServerCallbacks(ServerConfig config)
	{
		_config = config;
	}
	
	/// <summary>
	/// Reads from file each time so we can add/remove tokens without restarting server
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	private bool IsAuthorised(HttpListenerRequest request)
	{
		var authToken = request.Headers[HttpRequestHeader.Authorization.ToString()] ?? string.Empty;
		
		_config.Refresh();

		if (_config.AuthTokens == null || _config.AuthTokens.Count == 0)
			return true;

		return _config.AuthTokens.Contains(authToken);
	}

	public async Task<ServerResponse> Get(HttpListenerContext context)
	{
		await Task.CompletedTask;

		var path = context.Request.Url?.LocalPath ?? string.Empty;

		switch (path)
		{
			case "/workspaces": return new Workspaces().Process();
			case "/info": return new ServerInfo(((IServerCallbacks)this).Server).Process();
			case "/commits": return new Commits(context.Request.QueryString).Process();
			default: return ServerResponse.Ok;
		}
	}

	public async Task<ServerResponse> Post(HttpListenerContext context)
	{
		if (!IsAuthorised(context.Request)) return ServerResponse.UnAuthorised;
		if (!context.Request.HasEntityBody) return ServerResponse.NoContent;
		
		var packet = await context.GetPostContentAsync<RemoteBuildPacket>();
		return packet.Process();
	}

	public async Task<ServerResponse> Put(HttpListenerContext context)
	{
		// if (!IsAuthorised(request)) return ServerResponse.UnAuthorised;
		if (!context.Request.HasEntityBody) return ServerResponse.NoContent;

		// get pipeline
		var pipelineId = ulong.Parse(context.Request.Headers["pipelineId"] ?? "0");
		if (!App.Pipelines.TryGetValue(pipelineId, out var buildPipeline))
			return new ServerResponse(HttpStatusCode.NotFound, $"{nameof(BuildPipeline)} not found with ID: {pipelineId}");

		// create file
		var fileName = context.Request.Headers["fileName"] ?? string.Empty;
		var buildPath = context.Request.Headers["buildPath"] ?? string.Empty;
		var path = Path.Combine(buildPipeline.Workspace.Directory, buildPath, fileName);
		var fileInfo = new FileInfo(path);
		fileInfo.Directory?.Create();
		// Logger.Log($"Writing File: {fileInfo.FullName}");

		// write to file
		await using var fs = fileInfo.Create();
		await context.Request.InputStream.CopyToAsync(fs);
		return new ServerResponse(HttpStatusCode.OK, "File uploaded successfully.");
	}
}