using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SocketServer.Messages;

namespace SocketServer;

public class Server(int port)
{
    private readonly TcpListener listener = new(IPAddress.Any, port);
    private readonly Dictionary<uint, Client> _connectedClients = [];
    private static uint NextClientId;

    private readonly Dictionary<string, IService> _services = [];

    private static OperationSystemType OsType
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return OperationSystemType.Windows;
            if (OperatingSystem.IsMacOS())
                return OperationSystemType.MacOS;
            if (OperatingSystem.IsLinux())
                return OperationSystemType.Linux;
            throw new NotSupportedException("Operating system not supported");
        }
    }

    #region Core

    public async void Start()
    {
        listener.Start();
        Console.WriteLine($"[Server] Started on port {port}...");
        Console.WriteLine("Services:");
        foreach (var service in _services)
            Console.WriteLine($"  - {service.Key}");

        // PingClients();

        while (true)
        {
            var tpcClient = await listener.AcceptTcpClientAsync();
            var client = new Client(tpcClient);
            _connectedClients.Add(client.Id, client);
            Console.WriteLine(
                $"[Server] Client connected: {client.Id}, ConnectedClients: {_connectedClients.Count}"
            );

            await SendConnectionHandshake(client);
            HandleClient(client);
        }
    }

    private async Task SendConnectionHandshake(Client client)
    {
        var services = _services.Keys.ToArray();
        var message = new ServerConnectionMessage
        {
            ClientId = client.Id,
            MachineName = Environment.MachineName,
            OperatingSystem = OsType,
            Services = services
        };
        var data = Encoding.UTF8.GetBytes(message.ToString());
        await SendToClient(client, new TpcPacket(MessageType.Connection, data));
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

            Console.WriteLine($"[Server] Packet Received: {packet}");

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
                    ReceiveString(client, packet.ServiceName, str);
                    break;
                case MessageType.Binary:
                    ReceiveBinary(client, packet.ServiceName, packet.Data);
                    break;
                case MessageType.Json:
                    var json = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                    var jObject = JObject.Parse(json) ?? throw new NullReferenceException();
                    ReceiveJson(client, packet.ServiceName, jObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"Packet type not recognized. {packetType}"
                    );
            }
        }

        _connectedClients.Remove(inClient.Id);
    }

    #endregion

    #region Reived from clients

    private void ReceiveString(TcpClient client, string serviceName, string str)
    {
        Console.WriteLine($"[Server/{serviceName}] Received string: {str}");
        GetService(serviceName).OnStringMessage(str);
    }

    private void ReceiveBinary(TcpClient client, string serviceName, byte[] data)
    {
        Console.WriteLine($"[Server] Received byte[]: {data.Length}");
        GetService(serviceName).OnDataMessage(data);
    }

    private void ReceiveJson(TcpClient client, string serviceName, JObject json)
    {
        Console.WriteLine($"[Server] Received json: {json}");
        GetService(serviceName).OnJsonMessage(json);
    }

    private IService GetService(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var service))
            throw new Exception($"[Server] Service not found '{serviceName}'");

        return service;
    }

    #endregion

    #region Sends

    private async void PingClients()
    {
        while (true)
        {
            await Task.Delay(1000);
            await SendToClients(new TpcPacket(MessageType.String, "Ping"u8.ToArray()));
        }
    }

    internal async Task SendToClients(TpcPacket packet)
    {
        var tasks = _connectedClients.Select(client => SendToClient(client.Value, packet));
        await Task.WhenAll(tasks);
    }

    private static async Task SendToClient(Client client, TpcPacket packet)
    {
        var stream = client.TcpClient.GetStream();
        var data = packet.GetBytes();
        await stream.WriteAsync(data);
    }

    #endregion

    #region Services

    public void AddService(IService service)
    {
        _services.Add(service.Name, service);
    }

    public void RemoveService(string name)
    {
        _services.Remove(name);
    }

    #endregion

    private class Client(TcpClient tcpClient)
    {
        public uint Id { get; } = ++NextClientId;
        public TcpClient TcpClient { get; } = tcpClient;
    }
}
