﻿using System.Net;
using System.Text;
using Deployment.Server;
using Server.RemoteBuild;
using SharedLib;

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
		if (_listener.IsListening)
			Logger.Log($"... Server listening on '{_ip}:{_port}'");
		else
			throw new Exception("Server died");
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
	/// <param name="authToken"></param>
	/// <returns></returns>
	private bool IsAuthorised(string authToken)
	{
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
			"PUT" => await HandlePut(request),
			"OPTIONS" => await HandleOptions(request),
			_ => new ServerResponse(HttpStatusCode.MethodNotAllowed, $"HttpMethod not supported: {request.HttpMethod}")
		};

		Respond(context, response.StatusCode, response.Data);
	}

	private async Task<ServerResponse> HandleOptions(HttpListenerRequest request)
	{
		// TODO: handle CORS
		return new ServerResponse(HttpStatusCode.OK, "ok");
	}

	private async Task<ServerResponse> HandleGet(HttpListenerRequest request)
	{
		await Task.CompletedTask;

		var path = request.RawUrl ?? string.Empty;

		switch (path)
		{
			case "/workspaces":
				var workspaces = Workspace.GetAvailableWorkspaces().Select(x => x.Name).ToArray();
				return new ServerResponse(HttpStatusCode.OK, workspaces);
			
			case "/info": 
				return new ServerInfo(this).Process();
			
			default:
				return ServerResponse.Default;
		}
	}
	
	private static async Task<ServerResponse> HandlePut(HttpListenerRequest request)
	{
		if (!request.HasEntityBody)
			return new ServerResponse(HttpStatusCode.NoContent, "No body was given in request");

		using var reader = new BinaryReader(request.InputStream, request.ContentEncoding);
		var packet = new RemoteBuildResponse();
		packet.Read(reader);
		await Task.CompletedTask;
		return ProcessPacket(packet);
	}

	private async Task<ServerResponse> HandlePost(HttpListenerRequest request)
	{
		// check authorisation
		var authToken = request.Headers[HttpRequestHeader.Authorization.ToString()] ?? string.Empty;
		
		if (!IsAuthorised(authToken))
			return new ServerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action");

		if (!request.HasEntityBody)
			return new ServerResponse(HttpStatusCode.NoContent, "No body was given in request");
		
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