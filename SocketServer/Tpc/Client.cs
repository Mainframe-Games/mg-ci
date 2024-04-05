namespace SocketServer;

using System;
using System.Net.Sockets;
using System.Text;

public class Client
{
    private readonly TcpClient client;
    private readonly NetworkStream stream;

    public Client(string serverIp, int serverPort)
    {
        client = new TcpClient(serverIp, serverPort);
        stream = client.GetStream();
    }

    public async Task Send(string message)
    {
        Console.WriteLine($"Sent: {message}");
        var packet = new TpcPacket
        {
            Type = MessageType.String,
            Data = Encoding.UTF8.GetBytes(message)
        };
        var data = packet.GetBytes();
        await stream.WriteAsync(data);

        // Receive response from the server
        // var responseBuffer = new byte[1024];
        // var bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
        // var response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        // Console.WriteLine($"Response: {response}");
    }

    public async Task Send(byte[] bytes)
    {
        var packet = new TpcPacket
        {
            Type = MessageType.String,
            Data = bytes
        };
        var data = packet.GetBytes();
        await stream.WriteAsync(data);
    }

    public void Close()
    {
        var packet = new TpcPacket
        {
            IsClose = true
        };
        var data = packet.GetBytes();
        stream.Write(data);
        
        stream.Close();
        client.Close();
        
        stream.Dispose();
        client.Dispose();
    }
}