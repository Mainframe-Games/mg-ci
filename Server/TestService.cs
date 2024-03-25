using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace Server;

public class TestService : WebSocketBehavior
{
    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        Console.WriteLine($"OnClose: {e.Code} {e.Reason}");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        base.OnError(e);
        Console.WriteLine($"OnError: {e.Exception}");
        Console.WriteLine($"OnErrorMessage: {e.Message}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);
        Console.WriteLine($"OnMessage: {e.Data}");
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        Console.WriteLine("Test open");
    }
}