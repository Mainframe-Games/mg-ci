using System.Net;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using Server;

namespace SharedLib.Server;

public sealed class ListenServer
{
	private readonly string _ip;
	private readonly ushort _port;
	private readonly HttpListener _listener;
	private readonly Assembly _assembly;

	public string Prefixes => string.Join("\n", _listener.Prefixes);

	public DateTime ServerStartTime { get; }
	public bool IsListening => _listener.IsListening;

	public ListenServer(string ip, ushort port, Assembly assembly)
	{
		_ip = ip;
		_port = port;

		_assembly = assembly;

		_listener = new HttpListener();
		_listener.Prefixes.Add($"http://{_ip}:{_port}/");
		_listener.Start();

		ServerStartTime = DateTime.Now;
		
		Receive();
	}

	public void Stop()
	{
		_listener.Stop();
	}

	private void Receive()
	{
		// Logger.Log($"{nameof(ListenServer)} Address: {Prefixes}");
		_listener.BeginGetContext(ListenerCallback, _listener);
	}
	
	// TODO: implement authorisation
	/// <summary>
	/// Reads from file each time so we can add/remove tokens without restarting server
	/// </summary>
	/// <returns></returns>
	// private bool IsAuthorised(HttpListenerRequest request)
	// {
	// 	var authToken = request.Headers[HttpRequestHeader.Authorization.ToString()] ?? string.Empty;
	// 	
	// 	_config.Refresh();
	//
	// 	if (_config.AuthTokens == null || _config.AuthTokens.Count == 0)
	// 		return true;
	//
	// 	return _config.AuthTokens.Contains(authToken);
	// }

	private async void ListenerCallback(IAsyncResult result)
	{
		var context = _listener.EndGetContext(result);
		var response = new ServerResponse(HttpStatusCode.NotFound, $"404 Not Found '{context.Request.HttpMethod}' {context.Request.Url}");
		
		try
		{
			var endpoint = EndPointUtils.GetEndPoint(_assembly, context);
			if (endpoint is not null)
				response = await endpoint.ProcessAsync(this, context);
		}
		catch (Exception e)
		{
			response = new ServerResponse(HttpStatusCode.InternalServerError, new ErrorResponse(e));
		}

		Respond(context, response.StatusCode, response.Data);
	}

	private void Respond(HttpListenerContext context, HttpStatusCode responseCode, object? data)
	{
		var response = context.Response;
		string? resJson = null;
		
		try
		{
			response.StatusCode = (int)responseCode;
			response.ContentType = "application/json";

			if (data != null)
			{
				resJson = Json.Serialise(data);
				var bytes = Encoding.UTF8.GetBytes(resJson);
				response.OutputStream.Write(bytes);
			}

			response.OutputStream.Close();
		}
		catch (Exception e)
		{
			Logger.Log(e);
			Logger.Log($"resJson: {resJson ?? "{null}"}");
		}
		finally
		{
			Receive();
		}
	}
}