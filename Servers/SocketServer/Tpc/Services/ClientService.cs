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

    public async Task SendString(string message)
    {
        var packet = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(new TpcPacket(Name, MessageType.String, packet));
    }

    public async Task SendBinary(byte[] data)
    {
        await client.SendAsync(new TpcPacket(Name, MessageType.Binary, data));
    }

    public async Task SendJson(JObject payload)
    {
        var data = Encoding.UTF8.GetBytes(payload.ToString());
        var packet = new TpcPacket(Name, MessageType.Json, data);
        // Console.WriteLine($"[ClientService/{Name}] SendJson: {packet.Id}\n{payload}");
        await client.SendAsync(packet);
    }
}
