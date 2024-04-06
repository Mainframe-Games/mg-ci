using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SocketServer.Messages;
using SocketServer.Tests;

namespace SocketServer;

public sealed class Client
{
    private TcpClient client;
    private NetworkStream stream;
    public uint Id { get; private set; }
    public bool IsConnected => Id > 0 && client.Connected;

    /// <summary>
    /// The servers operating system we are connected to
    /// </summary>
    public OperationSystemType ServerOperationSystem { get; private set; }

    /// <summary>
    /// The servers machine name we are connected to
    /// </summary>
    public string ServerMachineName { get; set; } = string.Empty;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Dictionary<string, IService> _services = [];

    public Client(string serverIp, int serverPort)
    {
        // client = new TcpClient(serverIp, serverPort);
        // stream = client.GetStream();
        // var thread = new Thread(ListenForPackets);
        // thread.Start();
        Task.Run(() => Connect(serverIp, serverPort));
    }

    private async void Connect(string serverIp, int serverPort)
    {
        while (client?.Connected is not true)
        {
            try
            {
                client = new TcpClient(serverIp, serverPort);
                stream = client.GetStream();
                _ = Task.Run(ListenForPackets);
            }
            catch (SocketException)
            {
                // ignore as connection probably failed we should keep trying forever
            }

            await Task.Delay(1000);
        }
    }

    #region Sends

    internal async Task Send(TpcPacket packet)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Client is not connected");
            return;
        }

        var data = packet.GetBytes();
        await stream.WriteAsync(data);
    }

    #endregion

    #region Receives

    private void ReceiveConnectionHandshake(byte[] data)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data, 0, data.Length);
            var message = ServerConnectionMessage.Parse(json);
            Id = message.ClientId;
            ServerOperationSystem = message.OperatingSystem;
            ServerMachineName = message.MachineName ?? string.Empty;
            Console.WriteLine($"[Client_{Id}] Received connection from server: {message}");

            foreach (var service in _services)
            {
                if (service.Value is ClientService clientService)
                    clientService.OnConnected();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Kill();
        }
    }

    private async void ListenForPackets()
    {
        while (client.Connected)
        {
            try
            {
                while (!stream.DataAvailable) { }

                var buffer = new byte[client.ReceiveBufferSize];
                var bytesRead = await stream.ReadAsync(buffer, _cancellationTokenSource.Token);

                if (bytesRead == 0)
                    continue;

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                var packet = new TpcPacket();
                packet.Read(data);

                switch (packet.Type)
                {
                    case MessageType.Connection:
                        ReceiveConnectionHandshake(packet.Data);
                        break;

                    case MessageType.Close:
                        // ignore
                        break;

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
                            $"[Client_{Id}] Unknown packet type: {packet.Type}, packetId: {packet.Id}"
                        );
                        Kill();
                        break;
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                // ignore this if expected when client closes
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Kill();
            }
        }
    }

    private void ReceiveString(TcpClient tcpClient, string serviceName, string str)
    {
        Console.WriteLine($"[Client_{Id}/{serviceName}] Received string: {str}");
        GetService(serviceName).OnStringMessage(str);
    }

    private void ReceiveBinary(TcpClient tcpClient, string serviceName, byte[] data)
    {
        Console.WriteLine($"[Client_{Id}/{serviceName}] Received byte[]: {data.Length}");
        GetService(serviceName).OnDataMessage(data);
    }

    private void ReceiveJson(TcpClient tcpClient, string serviceName, JObject jObject)
    {
        Console.WriteLine($"[Client_{Id}/{serviceName}] Received json: {jObject}");
        GetService(serviceName).OnJsonMessage(jObject);
    }

    #endregion

    #region Services

    public void AddService(IService service)
    {
        _services.Add(service.Name, service);
    }

    public void RemoveService(string serviceName)
    {
        _services.Remove(serviceName);
    }

    private IService GetService(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var service))
            throw new Exception($"[Server] Service not found '{serviceName}'");

        return service;
    }

    #endregion

    private void Kill()
    {
        Console.WriteLine("[Client] Killing client...");
        Close();
        Environment.Exit(1);
    }

    public void Close()
    {
        var packet = new TpcPacket(MessageType.Close, Array.Empty<byte>());
        var data = packet.GetBytes();
        stream.Write(data);

        _cancellationTokenSource.Cancel();

        stream.Close();
        client.Close();

        stream.Dispose();
        client.Dispose();
    }
}
