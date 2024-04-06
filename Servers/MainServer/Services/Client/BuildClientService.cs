using Newtonsoft.Json.Linq;
using SocketServer;

namespace MainServer.Services.Client;

internal sealed class BuildClientService(SocketServer.Client client) : ClientService(client)
{
    public override string Name => "build";

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
        Console.WriteLine($"BuildClientService: Received json message: {payload}");
    }
}
