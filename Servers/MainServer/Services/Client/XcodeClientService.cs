using Newtonsoft.Json.Linq;
using SocketServer;

namespace MainServer.Services.Client;

public class XcodeClientService(SocketServer.Client client) : ClientService(client)
{
    public override string Name => "xcode";

    public override void OnStringMessage(string message)
    {
        throw new NotImplementedException();
    }

    public override void OnDataMessage(byte[] data)
    {
        throw new NotImplementedException();
    }

    public override void OnJsonMessage(JObject payload)
    {
        Console.WriteLine($"XcodeClientService.OnJsonMessage: {payload}");
    }
}
