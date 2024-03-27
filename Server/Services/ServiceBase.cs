using ServerClientShared;
using SharedLib;
using WebSocketSharp.Server;

namespace Server.Services;

public abstract class ServiceBase : WebSocketBehavior
{
    protected void Send(NetworkPayload payload)
    {
        var json = Json.Serialise(payload);
        Send(json);
    }
}