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
    Task SendString(string message);
    Task SendBinary(byte[] data);
    Task SendJson(JObject payload);
}
