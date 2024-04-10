using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SocketServer.Messages;

namespace SocketServer;

public sealed class Client
{
    private TcpClient _client;
    private NetworkStream _stream;
    public uint Id { get; private set; }
    public bool IsConnected => Id > 0 && _client.Connected;

    /// <summary>
    /// The servers operating system we are connected to
    /// </summary>
    public OperationSystemType ServerOperationSystem { get; private set; }

    /// <summary>
    /// The servers machine name we are connected to
    /// </summary>
    public string ServerMachineName { get; set; } = string.Empty;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public readonly Dictionary<string, ClientService> Services = [];

    public Client(string serverIp, int serverPort)
    {
        Task.Run(() => Connect(serverIp, serverPort));
    }

    private async void Connect(string serverIp, int serverPort)
    {
        while (_client?.Connected is not true)
        {
            try
            {
                _client = new TcpClient(serverIp, serverPort);
                _stream = _client.GetStream();
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

    private readonly Queue<TpcPacket> _sendQueue = new();
    private Task? _sendTask;

    internal void Send(TpcPacket packet)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Client is not connected");
            return;
        }

        _sendQueue.Enqueue(packet);

        if (_sendTask is null || _sendTask.IsCompleted)
            _sendTask = Task.Run(SendInternal);
    }

    private async Task SendInternal()
    {
        while (_sendQueue.Count > 0)
        {
            var packet = _sendQueue.Dequeue();
            var data = packet.GetBytes();
            Console.WriteLine($"[Client_{Id}] Send packet {packet}");
            await _stream.WriteAsync(data);
            await Task.Delay(20); // delay to prevent spamming
        }

        Console.WriteLine($"[Client_{Id}] Send queue empty");
        _sendTask = null;
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

            foreach (var serviceName in message.Services ?? [])
            {
                if (TryGetService(serviceName, out var service))
                    service?.OnConnected();
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
        while (_client.Connected)
        {
            try
            {
                while (!_stream.DataAvailable) { }

                var buffer = new byte[_client.ReceiveBufferSize];
                var bytesRead = await _stream.ReadAsync(buffer, _cancellationTokenSource.Token);

                if (bytesRead == 0)
                    continue;

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                var packet = new TpcPacket();
                packet.Read(data);
                
                Console.WriteLine($"[Client_{Id}] Packet Received: {packet}");

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
                        ReceiveString(_client, packet.ServiceName, str);
                        break;
                    case MessageType.Binary:
                        ReceiveBinary(_client, packet.ServiceName, packet.Data);
                        break;
                    case MessageType.Json:
                        var json = Encoding.UTF8.GetString(packet.Data, 0, packet.Data.Length);
                        var jObject = JObject.Parse(json) ?? throw new NullReferenceException();
                        ReceiveJson(_client, packet.ServiceName, jObject);
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
        // Console.WriteLine($"[Client_{Id}/{serviceName}] Received byte[]: {data.Length}");
        GetService(serviceName).OnDataMessage(data);
    }

    private void ReceiveJson(TcpClient tcpClient, string serviceName, JObject jObject)
    {
        Console.WriteLine($"[Client_{Id}/{serviceName}] Received json: {jObject}");
        GetService(serviceName).OnJsonMessage(jObject);
    }

    #endregion

    #region Services

    public void AddService(ClientService service)
    {
        Services.Add(service.Name, service);
    }

    private ClientService GetService(string serviceName)
    {
        if (!Services.TryGetValue(serviceName, out var service))
            throw new Exception($"[Client] Service not found '{serviceName}'");

        return service;
    }

    public bool TryGetService(string serviceName, out ClientService? service)
    {
        return Services.TryGetValue(serviceName, out service);
    }

    #endregion

    private void Kill()
    {
        Console.WriteLine($"[Client_{Id}] Killing client...");
        Close();
        Environment.Exit(1);
    }

    public void Close()
    {
        var packet = new TpcPacket(MessageType.Close, Array.Empty<byte>());
        var data = packet.GetBytes();
        _stream.Write(data);

        _cancellationTokenSource.Cancel();

        _stream.Close();
        _client.Close();

        _stream.Dispose();
        _client.Dispose();
    }
}
