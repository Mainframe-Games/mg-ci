using System.Net;
using System.Net.WebSockets;
using System.Text;
using SocketServer.Test;

namespace SocketServer;

public sealed class WebSocketServer
{
    private readonly HttpListener _listener;
    private readonly List<WebSocket> _connectedClients = [];

    public WebSocketServer(string url)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);
    }

    public async void Start()
    {
        _listener.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        // PingClients();

        while (true)
        {
            var context = await _listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private async void PingClients()
    {
        int i = 0;
        while (true)
        {
            await Task.Delay(1000);
            SendToClients($"Ping: {i++}");
        }
    }

    private async void ProcessWebSocketRequest(HttpListenerContext context)
    {
        var webSocketContext = await context.AcceptWebSocketAsync(null);
        var webSocket = webSocketContext.WebSocket;

        _connectedClients.Add(webSocket);

        while (webSocket.State is WebSocketState.Open)
        {
            var buffer = new byte[1024]; // Initial buffer size
            WebSocketReceiveResult result;

            Console.WriteLine("[Server] Waiting for incoming data...");
            do
            {
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (!result.EndOfMessage)
                    Array.Resize(ref buffer, buffer.Length * 2);
            } while (!result.EndOfMessage);

            Console.WriteLine(
                $"[Server] Received {result.MessageType} packet size: {Print.ToByteSizeString(result.Count)}"
            );

            // Process the received data
            if (result.MessageType == WebSocketMessageType.Text)
            {
                Console.WriteLine(
                    $"[Server] Received {result.MessageType} packet size: {Print.ToByteSizeString(result.Count)}"
                );
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"[Server] Received message length: {message.Length}");
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // Handle binary data
                var data = new byte[result.Count];
                Array.Copy(buffer, data, result.Count);
                FileDownload.Download(data);
            }
        }
    }

    private async void SendToClients(string message)
    {
        foreach (var client in _connectedClients)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await client.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
}
