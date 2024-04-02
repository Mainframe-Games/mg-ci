using WebSocketSharp;
using WebSocketSharp.Server;

namespace OffloadServer;

internal abstract class ServiceBase : WebSocketBehavior
{
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
    }
}
