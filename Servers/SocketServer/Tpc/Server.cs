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
            var tpcClient = await listener.AcceptTcpClientAsync();
            var client = new ConnectedClient(tpcClient);
            _connectedClients.Add(client.Id, client);
            Console.WriteLine(
                $"[Server] Client connected: {client.Id}, ConnectedClients: {_connectedClients.Count}"
            );

            SendConnectionHandshake(client);
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private void SendConnectionHandshake(ConnectedClient client)
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
        SendToClient(client, new TpcPacket(MessageType.Connection, data));
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
                    {
                        Console.WriteLine(
                            $"[Server] Unknown packet type: {packet.Type}, packetId: {packet.Id}"
                        );
                        Kill();
                        break;
                    }
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

    private void ReceiveString(TcpClient client, string serviceName, string str)
    {
        Console.WriteLine($"[Server/{serviceName}] Received string: {str}");
        GetService(serviceName).OnStringMessage(str);
    }

    private void ReceiveBinary(TcpClient client, string serviceName, byte[] data)
    {
        Console.WriteLine($"[Server/{serviceName}] Received byte[]: {data.Length}");
        GetService(serviceName).OnDataMessage(data);
    }

    private void ReceiveJson(TcpClient client, string serviceName, JObject json)
    {
        Console.WriteLine($"[Server/{serviceName}] Received json: {json}");
        GetService(serviceName).OnJsonMessage(json);
    }

    #endregion

    #region Sends

    private readonly Queue<(ConnectedClient, TpcPacket)> _sendQueue = [];
    private Task? _sendTask;

    internal void SendToClients(TpcPacket packet)
    {
        foreach (var client in _connectedClients)
            SendToClient(client.Value, packet);
    }

    private void SendToClient(ConnectedClient client, TpcPacket packet)
    {
        _sendQueue.Enqueue((client, packet));
        if (_sendTask is null || _sendTask.IsCompleted)
            _sendTask = Task.Run(SendInternal);
    }

    private async Task SendInternal()
    {
        while (_sendQueue.Count > 0)
        {
            var (client, packet) = _sendQueue.Dequeue();
            var stream = client.TcpClient.GetStream();
            var data = packet.GetBytes();
            Console.WriteLine($"[Server] Send packet {packet}");
            await stream.WriteAsync(data);
            await Task.Delay(20);
        }

        _sendTask = null;
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

    private class ConnectedClient(TcpClient tcpClient)
    {
        public uint Id { get; } = ++NextClientId;
        public TcpClient TcpClient { get; } = tcpClient;
    }
}
