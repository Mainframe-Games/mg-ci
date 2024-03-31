using SharedLib;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace ServerClientShared;

public class WebClient
{
    private const int WAIT_TIME = 60;
    private readonly WebSocket _ws;

    public bool IsAlive => _ws.IsAlive;
    public string Status => _ws.ReadyState.ToString();

    private readonly string? _path;

    public WebClient(string path, string? ip = "localhost", ushort port = 8080)
    {
        _path = path;

        _ws = new WebSocket($"ws://{ip}:{port}/{_path}");
        _ws.WaitTime = TimeSpan.FromSeconds(WAIT_TIME);

        _ws.OnOpen += OnOpen;
        _ws.OnMessage += OnMessage;
        _ws.OnError += OnError;
        _ws.OnClose += OnClose;
    }

    public void Connect()
    {
        _ws.Connect();
    }

    public void Close()
    {
        _ws.Close();
    }

    private void OnOpen(object? sender, EventArgs eventArgs)
    {
        Console.WriteLine($"Client connected: {_path}");
        // var payload = new NetworkPayload(MessageType.Connection, _clientId, "Hello");
        // var json = Json.Serialise(payload);
        // _ws.Send(json);
    }

    private void OnMessage(object? sender, MessageEventArgs e)
    {
        var body = !e.IsPing ? e.Data : "A ping was received.";
        Console.WriteLine($"[WebSocket Message] {body}");
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        Console.WriteLine($"[WebSocket Error] {e.Message}");
    }

    private void OnClose(object? sender, CloseEventArgs e)
    {
        Console.WriteLine($"[WebSocket Close ({e.Code})] {e.Reason}");
    }

    public void Send(object data)
    {
        if (_ws.ReadyState is not WebSocketState.Open)
        {
            Console.WriteLine("Socket is not open.");
            Connect();
        }

        var payload = new NetworkPayload(MessageType.Message, 69, data);
        var json = Json.Serialise(payload);
        _ws.Send(json);
    }
}
