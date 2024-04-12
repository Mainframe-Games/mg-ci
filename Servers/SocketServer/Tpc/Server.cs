using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SocketServer.Messages;

namespace SocketServer;

public class Server(int port)
{
    private readonly TcpListener listener = new(IPAddress.Any, port);
    private readonly Dictionary<uint, ConnectedClient> _connectedClients = [];
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

        while (true)
        {
            try
            {
                var tpcClient = await listener.AcceptTcpClientAsync();
                var client = new ConnectedClient(tpcClient);
                _connectedClients.Add(client.Id, client);
                Console.WriteLine(
                    $"[Server] Client connected: {client.Id}, ConnectedClients: {_connectedClients.Count}"
                );

                await SendConnectionHandshake(client);
                _ = Task.Run(() => HandleClient(client));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Kill();
            }
        }
    }

    private async Task SendConnectionHandshake(ConnectedClient client)
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

    private async void HandleClient(ConnectedClient inClient)
    {
        var client = inClient.TcpClient;
        var stream = client.GetStream();

        while (client.Connected)
        {
            try
            {
                while (!stream.DataAvailable) { }

                // // checksum
                // var sumBuffer = new byte[1024];
                // var sumSize = await stream.ReadAsync(sumBuffer);
                // var sumValue = new byte[sumSize];
                // Array.Copy(sumBuffer, sumValue, sumSize);
                // var sum = Encoding.UTF8.GetString(sumValue);

                // read size
                var sizeBuffer = new byte[sizeof(int)];
                var sizeBytesRead = await stream.ReadAsync(sizeBuffer);
                if (sizeBytesRead != sizeof(int))
                    throw new Exception("Failed to read packet size");

                var packetSize = BitConverter.ToInt32(sizeBuffer);
                // Console.WriteLine($"[Server] In coming packet size: {packetSize}");

                // read packet
                var buffer = new byte[packetSize];
                var bytesRead = 0;

                while (bytesRead < packetSize)
                    bytesRead += await stream.ReadAsync(buffer);

                if (bytesRead == 0)
                    continue;

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                var sumCalc = CheckSum.Build(data);
                // Console.WriteLine($"  Checksum: {sumCalc}");
                // Console.WriteLine($"  Checksum pass: {sumCalc == sum}");
                // Console.WriteLine($"  Size: {Print.ToByteSizeString(data.Length)}");
                // Console.WriteLine();

                var packet = new TpcPacket();
                packet.Read(data);

                // Console.WriteLine($"[Server] Packet Received: {packet}");

                var packetType = packet.Type;

                switch (packetType)
                {
                    case MessageType.Connection:
                        throw new Exception("Server should never receive a connection message.");

                    case MessageType.Close:
                        client.Close();
                        Console.WriteLine(
                            $"[Server] Client disconnected. ConnectedClients: {_connectedClients.Count}"
                        );
                        break;

                    case MessageType.String:
                        var str = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                        ReceiveString(packet.ServiceName, str);
                        break;
                    case MessageType.Binary:
                        ReceiveBinary(packet.ServiceName, packet.Data);
                        break;
                    case MessageType.Json:
                        var json = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                        var jObject = JObject.Parse(json) ?? throw new NullReferenceException();
                        ReceiveJson(packet.ServiceName, jObject);
                        break;
                    default:
                        throw new Exception(
                            $"[Server] Unknown packet type: {packet.Type}, packetId: {packet.Id}"
                        );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Kill();
            }
        }

        _connectedClients.Remove(inClient.Id);
    }

    #endregion

    #region Reived from clients

    private void ReceiveString(string serviceName, string str)
    {
        // Console.WriteLine($"[Server/{serviceName}] Received string: {str}");
        GetService(serviceName).OnStringMessage(str);
    }

    private void ReceiveBinary(string serviceName, byte[] data)
    {
        // Console.WriteLine($"[Server/{serviceName}] Received byte[]: {data.Length}");
        GetService(serviceName).OnDataMessage(data);
    }

    private void ReceiveJson(string serviceName, JObject json)
    {
        // Console.WriteLine($"[Server/{serviceName}] Received json: {json}");
        GetService(serviceName).OnJsonMessage(json);
    }

    #endregion

    #region Sends

    internal async Task SendToClients(TpcPacket packet)
    {
        await Task.WhenAll(_connectedClients.Select(client => SendToClient(client.Value, packet)));
    }

    private static async Task SendToClient(ConnectedClient client, TpcPacket packet)
    {
        await client.Dispatcher.SendAsync(packet);
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

    private IService GetService(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var service))
            throw new Exception($"[Server] Service not found '{serviceName}'");

        return service;
    }

    public T GetService<T>(string serviceName)
        where T : IService
    {
        if (!_services.TryGetValue(serviceName, out var service))
            throw new Exception($"[Server] Service not found '{serviceName}'");

        return (T)service;
    }

    #endregion

    private void Kill()
    {
        Console.WriteLine("[Server] Killing server...");
        Close();
        Environment.Exit(1);
    }

    public void Close()
    {
        listener.Stop();
        listener.Dispose();
    }

    private class ConnectedClient(TcpClient tcpClient) : INetworkDispatcher
    {
        public uint Id { get; } = ++NextClientId;
        public TcpClient TcpClient { get; } = tcpClient;
        public string Alias => $"Server -> Client_{Id}";
        public NetworkStream NetworkStream => TcpClient.GetStream();

        private PacketDispatcher? _dispatcher;
        public PacketDispatcher Dispatcher => _dispatcher ??= new PacketDispatcher(this);
    }
}
