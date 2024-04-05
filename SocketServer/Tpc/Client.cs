using Newtonsoft.Json.Linq;

namespace SocketServer;

using System;
using System.Net.Sockets;
using System.Text;

public class Client
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;

    public uint Id { get; private set; }
    public bool IsConnected => Id > 0 && client.Connected;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Client(string serverIp, int serverPort)
    {
        client = new TcpClient(serverIp, serverPort);
        stream = client.GetStream();
        var thread = new Thread(ListenForPackets);
        thread.Start();
    }

    #region Sends

    public async Task SendString(string message)
    {
        var packet = new TpcPacket(MessageType.String, Encoding.UTF8.GetBytes(message));
        await SendInternal(packet);
        Console.WriteLine($"[Client_{Id}] Sent string: {message}");
    }

    public async Task SendBinary(byte[] bytes)
    {
        var packet = new TpcPacket(MessageType.Binary, bytes);
        await SendInternal(packet);
        Console.WriteLine($"[Client_{Id}] Sent byte[]: {bytes.Length}");
    }

    public async Task SendJson(JObject jObject)
    {
        var packet = new TpcPacket(MessageType.Json, Encoding.UTF8.GetBytes(jObject.ToString()));
        await SendInternal(packet);
        Console.WriteLine($"[Client_{Id}] Sent json: {jObject}");
    }

    private async Task SendInternal(TpcPacket packet)
    {
        if (!IsConnected)
            throw new Exception("Client is not connected");

        var data = packet.GetBytes();
        await stream.WriteAsync(data);
    }

    #endregion

    #region Receives

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
                        Id = BitConverter.ToUInt32(packet.Data, 0);
                        Console.WriteLine($"[Client_{Id}] Received connection from server: {Id}");
                        break;

                    case MessageType.Close:
                        // ignore
                        break;

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
                        throw new ArgumentOutOfRangeException();
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
            }
        }
    }

    private void ReceiveString(TcpClient tcpClient, string str)
    {
        Console.WriteLine($"[Client_{Id}] Received string: {str}");
    }

    private void ReceiveBinary(TcpClient tcpClient, IReadOnlyCollection<byte> data)
    {
        Console.Write($"[Client_{Id}] Received byte[]: {data.Count}");
    }

    private void ReceiveJson(TcpClient tcpClient, JObject jObject)
    {
        Console.WriteLine($"[Client_{Id}] Received json: {jObject}");
    }

    #endregion

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
