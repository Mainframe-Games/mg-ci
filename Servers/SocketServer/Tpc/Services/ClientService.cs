using System.Text;
using Newtonsoft.Json.Linq;
using SocketServer.Messages;

namespace SocketServer;

public abstract class ClientService(Client client) : IService
{
    public abstract string Name { get; }

    internal virtual void OnConnected() { }

    public abstract void OnStringMessage(string message);
    public abstract void OnDataMessage(byte[] data);
    public abstract void OnJsonMessage(JObject payload);

    public bool IsConnected => client.IsConnected;
    public OperationSystemType ServerOperationSystem => client.ServerOperationSystem;

    public void SendString(string message)
    {
        var packet = Encoding.UTF8.GetBytes(message);
        client.Send(new TpcPacket(Name, MessageType.String, packet));
    }

    public void SendBinary(byte[] data)
    {
        client.Send(new TpcPacket(Name, MessageType.Binary, data));
    }

    public void SendJson(JObject payload)
    {
        var data = Encoding.UTF8.GetBytes(payload.ToString());
        var packet = new TpcPacket(Name, MessageType.Json, data);
        Console.WriteLine($"[ClientService] SendJson: {packet.Id}\n{payload}");
        client.Send(packet);
    }
}
