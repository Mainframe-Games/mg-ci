using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SocketServer.Test;

namespace SocketServer;

public class Server(int port)
{
    private readonly TcpListener listener = new(IPAddress.Any, port);
    private readonly Dictionary<uint, Client> _connectedClients = [];
    private static uint NextClientId;

    public async void Start()
    {
        listener.Start();
        Console.WriteLine($"[Server] Started on port {port}...");

        // PingClients();

        while (true)
        {
            var tpcClient = await listener.AcceptTcpClientAsync();
            var client = new Client(tpcClient);
            _connectedClients.Add(client.Id, client);
            Console.WriteLine(
                $"[Server] Client connected: {client.Id}, ConnectedClients: {_connectedClients.Count}"
            );
            SendToClient(
                client,
                new TpcPacket(MessageType.Connection, BitConverter.GetBytes(client.Id))
            );
            HandleClient(client);
        }
    }

    private async void HandleClient(object context)
    {
        var inClient = (Client)context;
        var client = inClient.TcpClient;
        var stream = client.GetStream();

        while (client.Connected)
        {
            while (!stream.DataAvailable) { }

            var buffer = new byte[client.ReceiveBufferSize];
            var bytesRead = await stream.ReadAsync(buffer);

            if (bytesRead == 0)
                continue;

            var data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);

            var packet = new TpcPacket();
            packet.Read(data);

            var packetType = packet.Type;

            switch (packetType)
            {
                case MessageType.Connection:
                    Console.WriteLine("Server should never receive a connection message.");
                    break;
                case MessageType.Close:
                {
                    client.Close();
                    Console.WriteLine(
                        $"[Server] Client disconnected. ConnectedClients: {_connectedClients.Count}"
                    );
                    break;
                }

                case MessageType.String:
                    var str = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                    ReceiveString(client, str);
                    break;
                case MessageType.Binary:
                    ReceiveBinary(client, packet.Data);
                    break;
                case MessageType.Json:
                    var json = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                    var jObject = JObject.Parse(json) ?? throw new NullReferenceException();
                    ReceiveJson(client, jObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Packet type not recognized. {packetType}"
                    );
            }
        }

        _connectedClients.Remove(inClient.Id);
    }

    private async void PingClients()
    {
        while (true)
        {
            await Task.Delay(1000);
            SendToClients(new TpcPacket(MessageType.String, "Ping"u8.ToArray()));
        }
    }

    #region Reived from clients

    private void ReceiveString(TcpClient client, string str)
    {
        Console.WriteLine($"[Server] Received string: {str}");

        // Echo back to the client
        // var response = Encoding.UTF8.GetBytes($"Server received: {str}");
        // stream.Write(response, 0, response.Length);
    }

    private void ReceiveBinary(TcpClient client, byte[] data)
    {
        Console.WriteLine($"[Server] Received byte[]: {data.Length}");
        FileDownload.Download(data);
    }

    private void ReceiveJson(TcpClient client, JObject json)
    {
        Console.WriteLine($"[Server] Received json: {json}");
    }

    #endregion

    private void SendToClients(TpcPacket packet)
    {
        foreach (var client in _connectedClients)
            SendToClient(client.Value, packet);
    }

    private static async void SendToClient(Client client, TpcPacket packet)
    {
        var stream = client.TcpClient.GetStream();
        var data = packet.GetBytes();
        await stream.WriteAsync(data);
    }

    private class Client(TcpClient tcpClient)
    {
        public uint Id { get; } = ++NextClientId;
        public TcpClient TcpClient { get; } = tcpClient;
    }
}
