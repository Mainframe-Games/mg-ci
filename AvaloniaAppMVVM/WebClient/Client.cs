using WebSocketSharp;

namespace AvaloniaAppMVVM.WebClient;

public class Client(string path)
{
    private const int WAIT_TIME = 60;
    
    private WebSocket? _ws;

    public bool IsAlive => _ws?.IsAlive ?? false;

    public void Connect()
    {
        Console.WriteLine($"Socket State: {_ws?.ReadyState}");

        if (IsAlive)
        {
            Console.WriteLine("Socket already connected.");
            return;
        }

        _ws = new WebSocket($"ws://localhost:8080/{path}");
        _ws.WaitTime = TimeSpan.FromSeconds(WAIT_TIME);
        
        _ws.OnOpen += (sender, e) => _ws.Send("Hi, from client");

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

    private async void PingLoop()
    {
        while (_ws?.IsAlive is true)
        {
            await Task.Delay(2000);
            Send("Ping");
        }
        
        Console.WriteLine("Ping loop end");
    }

    public void Send(string message)
    {
        if (_ws?.ReadyState is not WebSocketState.Open)
        {
            Console.WriteLine("Socket is not open.");
            Connect();
        }
        
        _ws?.Send(message);
    }

    public void Close()
    {
        _ws?.Close();
    }
}
