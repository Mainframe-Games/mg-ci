using AvaloniaAppMVVM.Utils;
using ServerClientShared;
using WebSocketSharp;

namespace AvaloniaAppMVVM.WebClient;

public class Client()
{
    private const int WAIT_TIME = 60;
    private static ushort _nextClientId;

    private WebSocket? _ws;

    public bool IsAlive => _ws?.IsAlive ?? false;
    public string Status => _ws?.ReadyState.ToString() ?? "Not connected";

    private readonly string? _path;
    private readonly ushort _clientId;

    public Client(string path)
        : this()
    {
        _clientId = _nextClientId++;
        _path = path;
    }

    public void Connect()
    {
        Console.WriteLine($"Socket State: {_ws?.ReadyState}");

        if (IsAlive)
        {
            Console.WriteLine("Socket already connected.");
            return;
        }

        _ws = new WebSocket($"ws://localhost:8080/{_path}");
        _ws.WaitTime = TimeSpan.FromSeconds(WAIT_TIME);

        _ws.OnOpen += (sender, e) =>
        {
            Console.WriteLine($"Client connected: {_path}");
        };

        _ws.OnMessage += (sender, e) =>
        {
            var body = !e.IsPing ? e.Data : "A ping was received.";
            Console.WriteLine("[WebSocket Message] {0}", body);
        };

        _ws.OnError += (sender, e) =>
        {
            Console.WriteLine("[WebSocket Error] {0}", e.Message);
        };

        _ws.OnClose += (sender, e) =>
        {
            Console.WriteLine("[WebSocket Close ({0})] {1}", e.Code, e.Reason);
        };

        _ws.Connect();
        PingLoop();
    }

    public void Close()
    {
        _ws?.Close();
    }

    private async void PingLoop()
    {
        while (_ws?.IsAlive is true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            Send([0]);
        }

        Console.WriteLine("Ping loop end");
    }

    private void Send(byte[] data)
    {
        if (_ws?.ReadyState is not WebSocketState.Open)
        {
            Console.WriteLine("Socket is not open.");
            Connect();
        }
        _ws?.Send(data);
    }

    public void Send(NetworkPayload payload)
    {
        if (_ws?.ReadyState is not WebSocketState.Open)
        {
            Console.WriteLine("Socket is not open.");
            Connect();
        }

        var json = Json.Serialise(payload);
        _ws?.Send(json);
    }
}
