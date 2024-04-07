using System.Text;
using Newtonsoft.Json.Linq;

namespace SocketServer;

public abstract class ServerService(Server server) : IService
{
    public abstract string Name { get; }
    public abstract void OnStringMessage(string message);
    public abstract void OnDataMessage(byte[] data);
    public abstract void OnJsonMessage(JObject payload);

    public void SendString(string message)
    {
        var packet = Encoding.UTF8.GetBytes(message);
        server.SendToClients(new TpcPacket(Name, MessageType.String, packet));
    }

    public void SendBinary(byte[] data)
    {
        server.SendToClients(new TpcPacket(Name, MessageType.Binary, data));
    }

    public void SendJson(JObject payload)
    {
        var data = Encoding.UTF8.GetBytes(payload.ToString());
        server.SendToClients(new TpcPacket(Name, MessageType.Json, data));
    }
}
