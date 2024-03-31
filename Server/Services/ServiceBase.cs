using ServerClientShared;
using SharedLib;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Server.Services;

public abstract class ServiceBase : WebSocketBehavior
{
    protected void Send(NetworkPayload payload)
    {
        var json = Json.Serialise(payload);
        Send(json);
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine($"Client connected [{Context.RequestUri.AbsolutePath}]: {ID}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        Console.WriteLine($"Client disconnected [{Context.RequestUri.AbsolutePath}]: {ID}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);
        Console.WriteLine($"Message received [{Context.RequestUri.AbsolutePath}]: {ID}\n{e.Data}");
        var json = Json.Deserialise<NetworkPayload>(e.Data) ?? throw new NullReferenceException();
        OnMessage(json);
    }

    protected abstract void OnMessage(NetworkPayload payload);
}
