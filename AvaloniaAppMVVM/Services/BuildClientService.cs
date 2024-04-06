using Newtonsoft.Json.Linq;
using SocketServer;

namespace AvaloniaAppMVVM.Services;

public class BuildClientService(Client client) : ClientService(client)
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
        throw new NotImplementedException();
    }
}
