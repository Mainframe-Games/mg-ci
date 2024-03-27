using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace Server.Services;

public class TestService : ServiceBase
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

        if (e.IsPing)
        {
            Send("Pong");
        }
        else if (e.IsBinary)
        {
            Console.WriteLine($"OnMessage Binary: {e.RawData}, length: {e.RawData.Length}");
        }
        else if (e.IsText)
        {
            Console.WriteLine($"OnMessage Text: {e.Data}");
        }
        else
        {
            Console.WriteLine($"OnMessage None: {e.Data}");
        }
    }
}
