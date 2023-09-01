using System.Net;
using System.Text;

namespace SharedLib.Server;

public class ListenServer
{
	private readonly string _ip;
	private readonly ushort _port;
	private readonly HttpListener _listener;
	private readonly IServerCallbacks _callbacks;

	public string Prefixes => string.Join("\n", _listener.Prefixes);

	public DateTime ServerStartTime { get; }
	public bool IsListening => _listener.IsListening;

	public ListenServer(string ip, ushort port, IServerCallbacks callbacks)
	{
		_ip = ip;
		_port = port;
		_callbacks = callbacks;
		_callbacks.Server = this;

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

	private async void ListenerCallback(IAsyncResult result)
	{
		var context = _listener.EndGetContext(result);
		ServerResponse response;
		
		try
		{
			// Logger.Log($"Incoming request: {context.Request.HttpMethod} {context.Request.Url}");
			response = context.Request.HttpMethod switch
			{
				"GET" => await _callbacks.Get(context),
				"POST" => await _callbacks.Post(context),
				"PUT" => await _callbacks.Put(context),
				_ => new ServerResponse(HttpStatusCode.MethodNotAllowed, $"HttpMethod not supported: {context.Request.HttpMethod}")
			};
		}
		catch (Exception e)
		{
			response = new ServerResponse(HttpStatusCode.InternalServerError, $"{e.GetType().Name}: {e.Message}");
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
			Receive();
		}
		catch (Exception e)
		{
			Logger.Log(e);
			Logger.Log($"resJson: {resJson ?? "{null}"}");
		}
		finally
		{
			response.OutputStream.Close();
			Receive();
		}
	}
}