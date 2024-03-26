using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace Server.Services;

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
        throw e.Exception;
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        base.OnMessage(e);
        Console.WriteLine($"OnMessage: {e.Data}");
        
        if (e.Data == "Ping")
            Send("Pong");
        else
            Send("Message from server");
    }
}
