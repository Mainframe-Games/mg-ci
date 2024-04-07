using Newtonsoft.Json.Linq;

namespace SocketServer;

public interface IService
{
    string Name { get; }

    // receives
    void OnStringMessage(string message);
    void OnDataMessage(byte[] data);
    void OnJsonMessage(JObject payload);

    // sends
    void SendString(string message);
    void SendBinary(byte[] data);
    void SendJson(JObject payload);
}
