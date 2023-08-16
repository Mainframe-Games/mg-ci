using System.Net;
using System.Text;
using Deployment.Server;
using Server.RemoteBuild;
using SharedLib;
using System;
using Deployment;

namespace Server;

public class ListenServer
{
	private readonly HttpListener _listener;
	public Func<List<string>>? GetAuth { get; set; }

	private readonly string _ip;
	private readonly ushort _port;

	public DateTime ServerStartTime { get; }

	public ListenServer(string ip, ushort port = 8080)
	{
		_ip = ip;
		_port = port;

		_listener = new HttpListener();
		_listener.Prefixes.Add($"http://{ip}:{port}/");
		_listener.Start();
		
		ServerStartTime = DateTime.Now;
		
		CheckIfServerStillListening();
	}

	public void CheckIfServerStillListening()
	{
		if (!_listener.IsListening)
			throw new Exception("Server died");

		Logger.Log($"[Server] Listening on '{_ip}:{_port}'");
		Logger.Log($"[Server] Version: {App.Version}");
	}

	public async Task RunAsync()
	{
		Receive();
		await Task.Delay(-1);
	}

	public void Stop()
	{
		_listener.Stop();
	}

	private void Receive()
	{
		_listener.BeginGetContext(ListenerCallback, _listener);
	}

	/// <summary>
	/// Reads from file each time so we can add/remove tokens without restarting server
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	private bool IsAuthorised(HttpListenerRequest request)
	{
		var authToken = request.Headers[HttpRequestHeader.Authorization.ToString()] ?? string.Empty;
		
		// always return true if no auths have been given
		var authTokens = GetAuth?.Invoke();
		
		if (authTokens == null || authTokens.Count == 0)
			return true;

		foreach (var token in authTokens)
		{
			if (token == authToken)
				return true;
		}

		return false;
	}

	private async void ListenerCallback(IAsyncResult result)
	{
		if (!_listener.IsListening)
			return;

		var context = _listener.EndGetContext(result);
		var request = context.Request;
		var response = request.HttpMethod switch
		{
			"GET" => await HandleGet(request),
			"POST" => await HandlePost(request),
			"PUT" => await HandlePut(context),
			_ => new ServerResponse(HttpStatusCode.MethodNotAllowed, $"HttpMethod not supported: {request.HttpMethod}")
		};

		Respond(context, response.StatusCode, response.Data);
	}

	private async Task<ServerResponse> HandleGet(HttpListenerRequest request)
	{
		await Task.CompletedTask;

		var path = request.Url?.LocalPath ?? string.Empty;

		switch (path)
		{
			case "/workspaces": return new Workspaces().Process();
			case "/info": return new ServerInfo(this).Process();
			case "/commits": return new Commits(request.QueryString).Process();
			default: return ServerResponse.Ok;
		}
	}
	
	private async Task<ServerResponse> HandlePut(HttpListenerContext context)
	{
		var request = context.Request;
		
		// if (!IsAuthorised(request)) return ServerResponse.UnAuthorised;
		if (!request.HasEntityBody) return ServerResponse.NoContent;

		try
		{
			// get pipeline
			var pipelineId = ulong.Parse(request.Headers["pipelineId"] ?? "0");
			if (!App.Pipelines.TryGetValue(pipelineId, out var buildPipeline))
				return new ServerResponse(HttpStatusCode.NotFound, $"{nameof(BuildPipeline)} not found with ID: {pipelineId}");
            
			// create file
			var fileName = request.Headers["fileName"] ?? string.Empty;
			var buildPath = request.Headers["buildPath"] ?? string.Empty;
			var path = Path.Combine(buildPipeline.Workspace.Directory, buildPath, fileName);
			var fileInfo = new FileInfo(path);
			fileInfo.Directory?.Create();
			Logger.Log($"Writing File: {fileInfo.FullName}");
			
			// write to file
			await using var fs = fileInfo.Create();
			await request.InputStream.CopyToAsync(fs);
			return new ServerResponse(HttpStatusCode.OK, "File uploaded successfully.");
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}

    private async Task<ServerResponse> HandlePost(HttpListenerRequest request)
	{
		if (!IsAuthorised(request)) return ServerResponse.UnAuthorised;
		if (!request.HasEntityBody) return ServerResponse.NoContent;
		
		using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
		var jsonStr = await reader.ReadToEndAsync();
		
		var packet = Json.Deserialise<RemoteBuildPacket>(jsonStr);
		if (packet == null)
			throw new NullReferenceException($"{nameof(RemoteBuildPacket)} is null from json: {jsonStr}");

		return ProcessPacket(packet);
	}

	private static ServerResponse ProcessPacket(IRemoteControllable packet)
	{
		try
		{
			return packet.Process();
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return new ServerResponse(HttpStatusCode.InternalServerError, e.Message);
		}
	}

	private void Respond(HttpListenerContext context, HttpStatusCode responseCode, object? data)
	{
		try
		{
			var response = context.Response;
			response.StatusCode = (int)responseCode;
			response.ContentType = "application/json";
			
			if (data != null)
			{
				var resJson = Json.Serialise(data);
				var bytes = Encoding.UTF8.GetBytes(resJson);
				response.OutputStream.Write(bytes);
			}
			
			response.OutputStream.Close();

			// start listening again
			Receive();
		}
		catch (Exception e)
		{
			Logger.Log(e);
		}
	}
}