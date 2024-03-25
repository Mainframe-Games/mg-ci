using WebSocketSharp;

namespace AvaloniaAppMVVM.WebClient;

public class Client
{
    public Client()
    {
        using var ws = new WebSocket ("ws://localhost:8080");
        
        ws.OnMessage += (sender, e) =>
            Console.WriteLine ("Laputa says: " + e.Data);

        ws.Connect ();
        ws.Send ("BALUS");
    }
}