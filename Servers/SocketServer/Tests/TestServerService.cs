using Newtonsoft.Json.Linq;

namespace SocketServer.Tests;

public class TestServerService(Server server) : ServerService(server)
{
    public override string Name => "test";

    public override void OnStringMessage(string message)
    {
        throw new NotImplementedException();
    }

    public override void OnDataMessage(byte[] data)
    {
        FileDownloader.Download(data);
    }

    public override void OnJsonMessage(JObject payload)
    {
        throw new NotImplementedException();
    }
}
