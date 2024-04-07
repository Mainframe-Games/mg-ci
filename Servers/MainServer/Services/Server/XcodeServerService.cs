using MainServer.Services.Packets;
using Newtonsoft.Json.Linq;
using SocketServer;
using XcodeDeployment;

namespace MainServer.Services.Server;

public class XcodeServerService(SocketServer.Server server) : ServerService(server)
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
        var data = payload.ToObject<XcodeDeployPacket>() ?? throw new NullReferenceException();
        var xcode = new XcodeDeploy(data.ProjectGuid, data.AppleId!, data.AppSpecificPassword!);
        xcode.Deploy();
    }
}
