using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer;

public enum MessageType : byte
{
    String,
    Binary
}

public class Server(int port)
{
    private readonly TcpListener listener = new(IPAddress.Any, port);
    private readonly List<TcpClient> _clients = [];

    public async void Start()
    {
        listener.Start();
        Console.WriteLine("Server started. Waiting for connections...");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");
            _clients.Add(client);

            var clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    private async void HandleClient(object clientObj)
    {
        var client = (TcpClient)clientObj;
        var stream = client.GetStream();

        while (client.Connected)
        {
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);

            if (bytesRead == 0)
                continue;

            var data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);

            var packet = new TpcPacket();
            packet.Read(data);

            if (packet.IsClose)
                break;

            switch (packet.Type)
            {
                case MessageType.String:
                    var str = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                    Console.WriteLine($"Received: {str}");

                    // Echo back to the client
                    // var response = Encoding.UTF8.GetBytes($"Server received: {str}");
                    // stream.Write(response, 0, response.Length);
                    break;
                case MessageType.Binary:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _clients.Remove(client);
        client.Close();
        Console.WriteLine("Client disconnected.");
    }
}